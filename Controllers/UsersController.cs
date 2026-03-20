using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using NUTRIBITE.Models;
using NUTRIBITE.Models.Users;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Identity;

namespace NUTRIBITE.Controllers
{
    public partial class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser>? _userManager;
        private readonly SignInManager<IdentityUser>? _signInManager;
        private const int PageSize = 10;

<<<<<<< HEAD
        [ActivatorUtilitiesConstructor]
        public UsersController(ApplicationDbContext context)
=======
        public UsersController(
            ApplicationDbContext context,
            UserManager<IdentityUser>? userManager = null,
            SignInManager<IdentityUser>? signInManager = null)
>>>>>>> 694cee74928822038d14aaba15f656ca2bf31689
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // INDEX: supports search, status filter and pagination
        [HttpGet]
        public IActionResult Index(string search = "", string status = "All", int page = 1)
        {
            var query = _context.UserSignups.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(u => u.Name.Contains(s) || u.Email.Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(u => (u.Status ?? "Active") == status);
            }

            int total = query.Count();

            var users = query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((Math.Max(1, page) - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // statistics
            ViewBag.TotalUsers = _context.UserSignups.Count();
            ViewBag.ActiveUsers = _context.UserSignups.Count(u => (u.Status ?? "Active") == "Active");
            ViewBag.BlockedUsers = _context.UserSignups.Count(u => u.Status == "Blocked");
            var weekStart = DateTime.Today.AddDays(-7);
            ViewBag.NewUsersThisWeek = _context.UserSignups.Count(u => (u.CreatedAt ?? DateTime.MinValue) >= weekStart);

            ViewBag.Page = Math.Max(1, page);
            ViewBag.PageSize = PageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / PageSize);
            ViewBag.Search = search ?? "";
            ViewBag.Status = status ?? "All";

            return View(users);
        }

        // Blocked users page (server-rendered)
        [HttpGet]
        public IActionResult BlockedUsers()
        {
            // Return list of blocked users to the BlockedUsers.cshtml view
            var blocked = _context.UserSignups
                .Where(u => u.Status == "Blocked")
                .OrderByDescending(u => u.CreatedAt)
                .ToList();

            return View(blocked);
        }

        // DETAILS
        [HttpGet]
        public IActionResult Details(int id)
        {
            var user = _context.UserSignups.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        // EDIT - GET
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var user = _context.UserSignups.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        // EDIT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, string Name, string Email, string? Phone, string? Status)
        {
            var user = _context.UserSignups.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            user.Name = Name ?? user.Name;
            user.Email = Email ?? user.Email;
            user.Phone = Phone;
            user.Status = string.IsNullOrWhiteSpace(Status) ? user.Status : Status;
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // BLOCK (GET) - simple admin action (keeps UI navigation simple)
        [HttpGet]
        public IActionResult BlockUser(int id)
        {
            var user = _context.UserSignups.Find(id);
            if (user != null)
            {
                user.Status = "Blocked";
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // UNBLOCK (GET)
        [HttpGet]
        public IActionResult UnblockUser(int id)
        {
            var user = _context.UserSignups.Find(id);
            if (user != null)
            {
                user.Status = "Active";
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // DELETE (GET) - confirmation should be client-side; server removes record
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var user = _context.UserSignups.Find(id);
            if (user != null)
            {
                _context.UserSignups.Remove(user);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // Existing AJAX endpoints remain unchanged (GetUsersData, GetUserOrdersData, etc.)
        [HttpGet]
        public IActionResult GetCalorieAnalyticsData(DateTime? from, DateTime? to)
        {
            var uid = HttpContext.Session.GetInt32("UserId");

            if (uid == null)
                return Json(null);

            var start = from ?? DateTime.Today.AddDays(-13);
            var end = to ?? DateTime.Today;

            var entries = _context.DailyCalorieEntries
                .Where(x => x.UserId == uid.Value &&
                            x.Date >= start &&
                            x.Date <= end)
                .ToList();

            var trend = entries
                .GroupBy(x => x.Date)
                .Select(g => new
                {
                    label = g.Key.ToString("dd MMM"),
                    calories = g.Sum(x => x.Calories)
                })
                .OrderBy(x => x.label)
                .ToList();

            var todayCalories = entries
                .Where(x => x.Date == DateTime.Today)
                .Sum(x => x.Calories);

            // 🔥 Get recommended calories from Health Survey
            var survey = _context.HealthSurveys
                .FirstOrDefault(x => x.UserId == uid.Value);

            int recommended = (int)(survey.Bmr ?? 2000);

            if (survey != null && survey.Bmr != null)
            {
                recommended = (int)survey.Bmr;
            }

            int avg = trend.Any() ? (int)trend.Average(x => x.calories) : 0;
            int peak = trend.Any() ? trend.Max(x => x.calories) : 0;

            int comparison = recommended > 0
                ? (int)((todayCalories * 100.0) / recommended)
                : 0;

           
            return Json(new
            {
                userName = HttpContext.Session.GetString("UserName"),

                todayCalories = todayCalories,
                recommendedDaily = recommended,
                averageDaily = avg,
                peakDaily = peak,
                comparisonPercent = comparison,
                trend = trend
            });
        }
    }
}