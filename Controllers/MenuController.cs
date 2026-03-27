using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NUTRIBITE.Models;

namespace NUTRIBITE.Controllers
{
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MenuController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================
        // GET: /Menu
        // Show all meal categories
        // ============================
        public IActionResult Index()
        {
            var categories = _context.AddCategories
                .Where(c => c.MealCategory != null)
                .OrderBy(c => c.MealCategory)
                .ToList();

            return View(categories);
        }

        // ============================

        // Show foods inside category
        // ============================
        public IActionResult Category(string name)
        {
            if (string.IsNullOrEmpty(name))
                return NotFound();

            var category = _context.AddCategories
                .AsEnumerable()   // 👈 IMPORTANT
                .FirstOrDefault(c =>
                    c.MealCategory.Trim().ToLower()
                    == name.Trim().ToLower());

            if (category == null)
                return Content("Category Not Found: " + name);

            var foods = _context.Foods
                .Include(f => f.Nutritionist)
                .Where(f => f.CategoryId == category.Cid) // Filter by CategoryId (AddCategory)
                .Take(3)
                .ToList();

            // Auto-seed demo data if category is empty
            if (!foods.Any())
            {
                var systemVendor = _context.VendorSignups.FirstOrDefault(v => v.Email == "system@nutribite.com");
                if (systemVendor == null)
                {
                    systemVendor = new VendorSignup 
                    { 
                        VendorName = "NutriBite Kitchen", 
                        Email = "system@nutribite.com", 
                        PasswordHash = "system_protected", 
                        IsApproved = true, 
                        CreatedAt = DateTime.Now,
                        Phone = "0000000000",
                        Address = "NutriBite Headquarters"
                    };
                    _context.VendorSignups.Add(systemVendor);
                    _context.SaveChanges();
                }

                var demoFoods = new List<Food>();
                string catName = category.MealCategory;

                // Create 3 demo items for this category
                for (int i = 1; i <= 3; i++)
                {
                    demoFoods.Add(new Food
                    {
                        Name = $"{catName} - Option {i}",
                        Description = $"A delicious and healthy {catName} prepared with fresh ingredients.",
                        Price = 120 + (i * 20),
                        Calories = 450 + (i * 50),
                        Protein = 15 + (i * 2),
                        Carbs = 50 + (i * 5),
                        Fat = 10 + i,
                        PreparationTime = "25 mins",
                        FoodType = "Veg",
                        ImagePath = $"/images/Meals/Standard_Thali.jpeg", // Fallback image
                        VendorId = systemVendor.VendorId,
                        Status = "Active",
                        CreatedAt = DateTime.Now,
                        IsVerified = true,
                        CategoryId = category.Cid,
                        // We leave MealCategoryId null to avoid Foreign Key constraint errors 
                        // with the legacy MealCategory table from the bulk insert script
                        MealCategoryId = null 
                    });
                }

                // Customize images/names for specific categories
                if (catName.Contains("Special"))
                {
                    foreach (var f in demoFoods) f.ImagePath = "/images/Meals/Special_Thali.jpeg";
                }
                else if (catName.Contains("Deluxe"))
                {
                    foreach (var f in demoFoods) f.ImagePath = "/images/Meals/Deluxe_Thali.jpeg";
                }
                else if (catName.Contains("Rice"))
                {
                    foreach (var f in demoFoods) f.ImagePath = "/images/Meals/Rice_Combo.jpeg";
                }

                _context.Foods.AddRange(demoFoods);
                _context.SaveChanges();
                foods = demoFoods;
            }

            ViewBag.CategoryName = category.MealCategory;

            return View(foods);
        }
        public IActionResult Test()
        {
            return Content("Menu Controller Working");
        }
    }
}