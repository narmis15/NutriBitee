using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using NUTRIBITE.ViewModels;
using NUTRIBITE.Services;
using NUTRIBITE.Models;

namespace NUTRIBITE.Controllers
{
    public class HealthSurveyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHealthCalculationService _calc;

        public HealthSurveyController(ApplicationDbContext context,
                                      IHealthCalculationService calc)
        {
            _context = context;
            _calc = calc;
        }

        // =========================
        // GET: /HealthSurvey
        // =========================
        [HttpGet]
        public IActionResult Index()
        {
            var uid = HttpContext.Session.GetInt32("UserId");

            if (!uid.HasValue)
                return RedirectToAction("Login", "Auth");

            var survey = _context.HealthSurveys
                .FirstOrDefault(h => h.UserId == uid.Value);

            // If survey exists → directly show Result
            if (survey != null)
                return RedirectToAction("Result");

            return View(new HealthSurveyViewModel());
        }


        // =========================
        // GET: /HealthSurvey/Edit
        // =========================
        [HttpGet]
        public IActionResult Edit()
        {
            var uid = HttpContext.Session.GetInt32("UserId");

            if (!uid.HasValue)
                return RedirectToAction("Login", "Auth");

            var survey = _context.HealthSurveys
                .FirstOrDefault(h => h.UserId == uid.Value);

            if (survey == null)
                return RedirectToAction("Index");

            var vm = new HealthSurveyViewModel
            {
                Age = survey.Age,
                Gender = survey.Gender,
                HeightCm = survey.HeightCm ?? 0,
                WeightKg = survey.WeightKg ?? 0,
                ActivityLevel = survey.ActivityLevel,
                Goal = survey.Goal,
                ChronicDiseases = survey.ChronicDiseases,
                FoodAllergies = survey.FoodAllergies,
                DietaryPreference = survey.DietaryPreference,
                Smoking = survey.Smoking,
                Alcohol = survey.Alcohol
            };

            return View("Index", vm);
        }


        // =========================
        // POST: /HealthSurvey
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(HealthSurveyViewModel model)
        {
            var uid = HttpContext.Session.GetInt32("UserId");

            if (!uid.HasValue)
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            var survey = _context.HealthSurveys
                .FirstOrDefault(h => h.UserId == uid.Value);

            var calcResult = _calc.Calculate(model);

            if (survey == null)
            {
                survey = new HealthSurvey
                {
                    UserId = uid.Value,
                    CreatedAt = DateTime.UtcNow
                };

                _context.HealthSurveys.Add(survey);
            }

            // Update survey values
            survey.Age = model.Age;
            survey.Gender = model.Gender ?? "";
            survey.HeightCm = model.HeightCm;
            survey.WeightKg = model.WeightKg;
            survey.ActivityLevel = model.ActivityLevel ?? "";
            survey.Goal = model.Goal ?? "";
            survey.ChronicDiseases = model.ChronicDiseases ?? "";
            survey.FoodAllergies = model.FoodAllergies ?? "";
            survey.DietaryPreference = model.DietaryPreference ?? "";
            survey.Smoking = model.Smoking;
            survey.Alcohol = model.Alcohol;

            // Calculated values
            survey.Bmi = calcResult.BMI;
            survey.Bmr = calcResult.BMR;
            survey.RecommendedCalories = calcResult.RecommendedCalories;
            survey.RecommendedProtein = calcResult.RecommendedProtein;

            _context.SaveChanges();

            return RedirectToAction("Result");
        }


        // =========================
        // GET: /HealthSurvey/Result
        // =========================
        [HttpGet]
        [HttpGet]
        public IActionResult Result()
        {
            var uid = HttpContext.Session.GetInt32("UserId");

            if (!uid.HasValue)
                return RedirectToAction("Login", "Auth");

            var survey = _context.HealthSurveys
                .FirstOrDefault(h => h.UserId == uid.Value);

            if (survey == null)
                return RedirectToAction("Index");

            List<Food> foods;

            // SMART FOOD RECOMMENDATION BASED ON GOAL
            if (survey.Goal == "Weight Loss")
            {
                foods = _context.Foods
                    .Where(f => f.Calories <= 450)
                    .OrderBy(f => f.Calories)
                    .Take(6)
                    .ToList();
            }
            else if (survey.Goal == "Muscle Gain")
            {
                foods = _context.Foods
                    .Where(f => f.Calories >= 600)
                    .OrderByDescending(f => f.Calories)
                    .Take(6)
                    .ToList();
            }
            else
            {
                foods = _context.Foods
                    .OrderBy(f => f.Calories)
                    .Take(6)
                    .ToList();
            }

            ViewBag.FoodSuggestions = foods;

            return View(survey);
        }
    }
}