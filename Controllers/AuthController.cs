using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using global::NUTRIBITE.Models;
using global::NUTRIBITE.Services;
using System.Threading.Tasks;

namespace NUTRIBITE.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IGoogleAuthService _googleAuthService;

        public AuthController(ApplicationDbContext context, IGoogleAuthService googleAuthService)
        {
            _context = context;
            _googleAuthService = googleAuthService;
        }

        // =========================
        // GET: /Auth/Login
        // =========================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // =========================
        // GET: /Auth/Register
        // =========================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // =========================
        // POST: /Auth/Register
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string name, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return Json(new { success = false, message = "All fields are required." });
            }

            var exists = _context.UserSignups
                .Any(u => u.Email == email.Trim());

            if (exists)
                return Json(new { success = false, message = "Email already exists." });

            var user = new UserSignup
            {
                Name = name.Trim(),
                Email = email.Trim(),
                Password = password,
                Role = "User", // ⭐ DEFAULT ROLE
                CreatedAt = DateTime.Now
            };

            _context.UserSignups.Add(user);
            _context.SaveChanges();

            // Set session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserRole", user.Role);

            var redirectUrl = Url.Action("Index", "HealthSurvey") ?? "/HealthSurvey";
            return Json(new { success = true, redirect = redirectUrl });
        }

        // =========================
        // CHECK AUTH (AJAX)
        // =========================
        [HttpGet]
        public IActionResult IsAuthenticated()
        {
            var uid = HttpContext.Session.GetInt32("UserId");

            if (!uid.HasValue)
                return Json(new { authenticated = false });

            var userName = HttpContext.Session.GetString("UserName") ?? "";
            var role = HttpContext.Session.GetString("UserRole") ?? "User";

            return Json(new { authenticated = true, userName, role });
        }

        // =========================
        // POST: /Auth/Login
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return Json(new { success = false, message = "Email and password are required." });
            }

            var user = _context.UserSignups
                .FirstOrDefault(u =>
                    u.Email == email.Trim() &&
                    u.Password == password);

            if (user == null)
                return Json(new { success = false, message = "Invalid email or password." });

            // ⭐ VENDOR APPROVAL CHECK
            if (user.Role == "Vendor" && user.Status != "Approved")
            {
                return Json(new { 
                    success = false, 
                    isPendingVendor = true,
                    message = "Your vendor account is pending approval. You can login only after the admin approves your request." 
                });
            }

            // ⭐ STORE ROLE IN SESSION
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Name ?? "");
            HttpContext.Session.SetString("UserRole", user.Role ?? "User");


            bool requiresSurvey = !_context.HealthSurveys
                .Any(h => h.UserId == user.Id);

            return Json(new
            {
                success = true,
                isAdmin = user.Role == "Admin",
                isVendor = user.Role == "Vendor",
                requiresSurvey,
                userName = user.Name
            });
        }

        // =========================
        // LOGOUT
        // =========================
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // =========================
        // GOOGLE AUTH
        // =========================
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var url = _googleAuthService.GetGoogleLoginUrl();
            return Redirect(url);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string code, string error)
        {
            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
            {
                TempData["LoginError"] = "Google authentication was cancelled or failed.";
                return RedirectToAction("Login");
            }

            var result = await _googleAuthService.AuthenticateUserAsync(code);

            if (!result.Success)
            {
                TempData["LoginError"] = result.Error;
                return RedirectToAction("Login");
            }

            // ⭐ DETECT DEMO MODE
            bool isDemoMode = code == "demo_code_123";

            var user = _context.UserSignups.FirstOrDefault(u => u.Email == result.Email);

            if (user == null)
            {
                user = new UserSignup
                {
                    Name = result.Name,
                    Email = result.Email,
                    Password = Guid.NewGuid().ToString("N"), // Random password for social users
                    Role = "User",
                    CreatedAt = DateTime.Now,
                    Status = "Approved"
                };
                _context.UserSignups.Add(user);
                await _context.SaveChangesAsync();
            }

            // Set session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserRole", user.Role);

            TempData["Success"] = isDemoMode 
                ? $"Demo Mode Active: Successfully logged in as '{user.Name}' ({user.Email})."
                : $"Welcome back, {user.Name}!";
            
            return RedirectToAction("Index", "Home");
        }

        // =========================
        // GET DEMO PROFILE (AJAX)
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetDemoProfile()
        {
            var result = await _googleAuthService.AuthenticateUserAsync("demo_code_123");
            if (result.Success)
            {
                return Json(new { 
                    success = true, 
                    email = result.Email, 
                    name = result.Name,
                    picture = result.Picture
                });
            }
            return Json(new { success = false, message = result.Error });
        }
    }
}