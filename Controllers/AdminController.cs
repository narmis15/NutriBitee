using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using global::NUTRIBITE.Hubs;
using global::NUTRIBITE.Services;
using global::NUTRIBITE.Models;
using global::NUTRIBITE.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace NUTRIBITE.Controllers
{
    public partial class AdminController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IHubContext<AnalyticsHub> _hubContext;
        private readonly IPaymentDistributionService _distributionService;
        private readonly ILogger<AdminController> _log;
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IActivityLogger _activityLogger;

        public AdminController(
            IConfiguration config,
            IOrderService orderService,
            ApplicationDbContext context,
            IPaymentDistributionService distributionService,
            IWebHostEnvironment env,
            IActivityLogger activityLogger,
            IHubContext<AnalyticsHub> hubContext,
            ILogger<AdminController> log)
        {
            _config = config;
            _orderService = orderService;
            _context = context;
            _distributionService = distributionService;
            _env = env;
            _activityLogger = activityLogger;
            _hubContext = hubContext;
            _log = log;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("Admin") != null)
                return RedirectToAction("Dashboard");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string? returnUrl, string password)
        {
            // Simple hardcoded admin check for now, as per typical prototype patterns
            if (email == "Nutribite123@gmail.com" && password == "NutriBite//26")
            {
                HttpContext.Session.SetString("Admin", email);
                HttpContext.Session.SetString("UserRole", "Admin");
                return RedirectToAction("Dashboard");
            }
            ViewBag.Error = "Invalid admin credentials.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [AdminAuthorize]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.RecentActivity = await _context.ActivityLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToListAsync();
            return View();
        }

        [AdminAuthorize]
        public IActionResult Profile()
        {
            var email = HttpContext.Session.GetString("Admin");
            if (email == null) return RedirectToAction("Login");
            return View();
        }

        [AdminAuthorize]
        public async Task<IActionResult> ManageVendor()
        {
            var vendors = await _context.VendorSignups.ToListAsync();
            return View(vendors);
        }

        [AdminAuthorize]
        public async Task<IActionResult> NewVendorRequest()
        {
            var pending = await _context.VendorSignups.Where(v => !v.IsApproved && !v.IsRejected).ToListAsync();
            return View(pending);
        }

        [AdminAuthorize]
        public IActionResult AddFoodCategory()
        {
            ViewBag.SpecifiedCategories = SpecifiedMealCategories;
            return View();
        }

        private static readonly string[] SpecifiedMealCategories = {
            "Low Calorie Meal", "Protein Meal", "Rice Combo", "Salads",
            "Classical Thali", "Comfort Thali", "Deluxe Thali",
            "Jain Thali", "Special Thali", "Standard Thali"
        };

        [AdminAuthorize]
        public async Task<IActionResult> Payouts()
        {
            var payouts = await _distributionService.GetAllPayoutsAsync();
            return View(payouts);
        }

        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePayoutStatus(int payoutId, PayoutStatus status)
        {
            try
            {
                var adminEmail = HttpContext.Session.GetString("Admin");
                await _distributionService.UpdatePayoutStatusAsync(payoutId, status, adminEmail ?? "Admin");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to update payout status");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMonthlyPayouts(int year, int month)
        {
            try
            {
                await _distributionService.ProcessMonthlyPayoutsAsync(year, month);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to generate monthly payouts");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [AdminAuthorize]
        public async Task<IActionResult> ViewCategory()
        {
            var categories = await _context.AddCategories
                .Where(c => !string.IsNullOrEmpty(c.ProductCategory))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(categories);
        }

        [AdminAuthorize]
        public async Task<IActionResult> ViewMealCategory()
        {
            var categoriesList = await _context.AddCategories
                .Where(c => !string.IsNullOrEmpty(c.MealCategory) && SpecifiedMealCategories.Contains(c.MealCategory))
                .ToListAsync();

            var uniqueCategories = categoriesList
                .GroupBy(c => c.MealCategory)
                .Select(g => g.OrderByDescending(c => !string.IsNullOrEmpty(c.MealPic)).ThenBy(c => c.Cid).First())
                .OrderBy(c => c.MealCategory)
                .ToList();

            return View(uniqueCategories);
        }

        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFoodCategory(string productCategory, string? customProductCategory, string mealCategory, string? customMealCategory, IFormFile categoryImage)
        {
            try
            {
                var finalProductCat = productCategory == "Other" ? customProductCategory : productCategory;
                var finalMealCat = mealCategory == "Other" ? customMealCategory : mealCategory;

                if (string.IsNullOrEmpty(finalProductCat))
                {
                    ViewBag.Error = "Product category is required.";
                    return View();
                }

                if (!string.IsNullOrEmpty(finalMealCat) && !SpecifiedMealCategories.Contains(finalMealCat))
                {
                    ViewBag.Error = $"Only specified meal categories are allowed: {string.Join(", ", SpecifiedMealCategories)}";
                    return View();
                }

                // Check for existing duplicate
                var existingCat = await _context.AddCategories
                    .FirstOrDefaultAsync(c => c.ProductCategory == finalProductCat && c.MealCategory == finalMealCat);

                if (existingCat != null)
                {
                    ViewBag.Error = "This category combination already exists.";
                    return View();
                }

                string fileName = "default.jpg";
                if (categoryImage != null && categoryImage.Length > 0)
                {
                    fileName = Guid.NewGuid().ToString() + Path.GetExtension(categoryImage.FileName);
                    // Determine where to save based on context - usually Product images
                    string folder = Path.Combine(_env.WebRootPath, "images", "Product");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    
                    string path = Path.Combine(folder, fileName);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await categoryImage.CopyToAsync(stream);
                    }

                    // Also save to Meals folder if it's a meal category
                    if (!string.IsNullOrEmpty(finalMealCat))
                    {
                        string mealFolder = Path.Combine(_env.WebRootPath, "images", "Meals");
                        if (!Directory.Exists(mealFolder)) Directory.CreateDirectory(mealFolder);
                        string mealPath = Path.Combine(mealFolder, fileName);
                        System.IO.File.Copy(path, mealPath, true);
                    }
                }

                var newCat = new AddCategory
                {
                    ProductCategory = finalProductCat,
                    ProductPic = fileName,
                    MealCategory = finalMealCat,
                    MealPic = fileName,
                    CreatedAt = DateTime.Now
                };

                _context.AddCategories.Add(newCat);
                await _context.SaveChangesAsync();
                await _activityLogger.LogAsync("Category Added", $"New category '{finalProductCat}' / '{finalMealCat}' created.");

                ViewBag.Success = "Category added successfully!";
                return View();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to add category");
                ViewBag.Error = "An error occurred while saving the category.";
                return View();
            }
        }

        [AdminAuthorize]
        public IActionResult DeliveryDashboard() => View();

        [AdminAuthorize]
        public IActionResult DeliveryOptimization() => View();
    }
}
