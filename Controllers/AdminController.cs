using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NutriBite.Filters;

namespace NUTRIBITE.Controllers
{
    [AdminAuthorize] // 🔒 whole controller protected
    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // 🔓 PUBLIC
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        // 🔓 PUBLIC
        [HttpPost]
        [AllowAnonymous]
        public IActionResult Login(string UserId, string Password)
        {
            if (UserId == "Nutribite123@gmail.com" &&
                Password == "NutriBite//26")
            {
                HttpContext.Session.SetString("Admin", UserId);
                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Invalid UserId or Password";
            return View();
        }

        // 🔒 PROTECTED
        public IActionResult Dashboard()
        {
            LoadDashboardCounts();
            return View();
        }

        // 🔓 PUBLIC (important)
        [AllowAnonymous]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private void LoadDashboardCounts()
        {
            string cs = _configuration.GetConnectionString("DBCS")
                        ?? throw new Exception("DBCS not found");

            using SqlConnection con = new SqlConnection(cs);
            con.Open();

            ViewBag.Users = GetValue(con, "SELECT COUNT(*) FROM UserSignup");
            ViewBag.Vendors = GetValue(con, "SELECT COUNT(*) FROM VendorSignup");
            ViewBag.Orders = GetValue(con, "SELECT COUNT(*) FROM OrderTable");
            ViewBag.Products = GetValue(con, "SELECT ISNULL(SUM(Qty),0) FROM OrderTable");
            ViewBag.TotalAmount = GetValue(con, "SELECT ISNULL(SUM(Amount),0) FROM Payment");
            ViewBag.Profit = GetValue(con, "SELECT ISNULL(SUM(Amount),0) * 0.10 FROM Payment");
        }

        private string GetValue(SqlConnection con, string query)
        {
            object result = new SqlCommand(query, con).ExecuteScalar();
            return result?.ToString() ?? "0";
        }
    }
}

