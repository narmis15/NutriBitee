using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Collections.Generic;
using NUTRIBITE.Models;

namespace NUTRIBITE.Controllers
{
    public class CalorieTrackerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CalorieTrackerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =============================
        // GET: /CalorieTracker
        // =============================
        [HttpGet]
        public IActionResult Index()
        {
            var uid = HttpContext.Session.GetInt32("UserId");
            if (!uid.HasValue)
                return RedirectToAction("Login", "Auth");

            var today = DateTime.Today;

            // Get survey for recommended calories
            var survey = _context.HealthSurveys
                .FirstOrDefault(h => h.UserId == uid.Value);

            int recommendedCalories = survey?.RecommendedCalories ?? 2000;

            // Calculate today's calories
            int todayCalories = _context.DailyCalorieEntries
                .Where(d => d.UserId == uid.Value && d.Date == today)
                .Sum(d => d.Calories);

            ViewBag.RecommendedCalories = recommendedCalories;
            ViewBag.TodayCalories = todayCalories;
            ViewBag.RemainingCalories = recommendedCalories - todayCalories;

            return View();
        }

        // =============================
        // POST: /CalorieTracker/AddEntry
        // =============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddEntry(string foodName, int calories, decimal protein = 0, decimal carbs = 0, decimal fats = 0)
        {
            var uid = HttpContext.Session.GetInt32("UserId");

            if (!uid.HasValue)
                return Json(new { success = false, message = "User not logged in" });

            if (string.IsNullOrWhiteSpace(foodName) || calories <= 0)
                return Json(new { success = false, message = "Invalid food entry" });

            var entry = new DailyCalorieEntry
            {
                UserId = uid.Value,
                Date = DateTime.Today,
                FoodName = foodName.Trim(),
                Calories = calories,
                Protein = protein,
                Carbs = carbs,
                Fats = fats
            };

            _context.DailyCalorieEntries.Add(entry);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        // =============================
        // GET: /CalorieTracker/GetTodayEntries
        // =============================
        [HttpGet]
        public IActionResult GetTodayEntries()
        {
            var uid = HttpContext.Session.GetInt32("UserId");

            if (!uid.HasValue)
                return Json(new { authenticated = false });

            var today = DateTime.Today;

            var entries = _context.DailyCalorieEntries
                .Where(d => d.UserId == uid.Value && d.Date == today)
                .OrderByDescending(d => d.Id)
                .Select(d => new
                {
                    id = d.Id,
                    food = d.FoodName,
                    calories = d.Calories,
                    protein = d.Protein ?? 0,
                    carbs = d.Carbs ?? 0,
                    fats = d.Fats ?? 0
                })
                .ToList();

            return Json(new
            {
                authenticated = true,
                items = entries
            });
        }

        // =============================
        // GET: /CalorieTracker/GetWeeklyData
        // =============================
        [HttpGet]
        public IActionResult GetWeeklyData()
        {
            var uid = HttpContext.Session.GetInt32("UserId");

            if (!uid.HasValue)
                return Json(new { authenticated = false });

            var startDate = DateTime.Today.AddDays(-6);

            var weekly = _context.DailyCalorieEntries
                .Where(d => d.UserId == uid.Value && d.Date >= startDate)
                .GroupBy(d => d.Date)
                .Select(g => new
                {
                    date = g.Key,
                    calories = g.Sum(x => x.Calories)
                })
                .OrderBy(g => g.date)
                .ToList();

            return Json(new
            {
                authenticated = true,
                data = weekly
            });
        }
    }
}