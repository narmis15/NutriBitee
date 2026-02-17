using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace NUTRIBITE.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Auth/Register
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

            try
            {
                string cs = _configuration.GetConnectionString("DBCS");

                using SqlConnection con = new SqlConnection(cs);
                con.Open();

                // Check if email already exists
                SqlCommand checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM UserSignup WHERE Email = @e", con);

                checkCmd.Parameters.AddWithValue("@e", email);

                int exists = (int)checkCmd.ExecuteScalar();

                if (exists > 0)
                {
                    return Json(new { success = false, message = "Email already registered." });
                }

                // Insert new user
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO UserSignup (Name, Email, Password) VALUES (@n, @e, @p)", con);

                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@e", email);
                cmd.Parameters.AddWithValue("@p", password);

                cmd.ExecuteNonQuery();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            string cs = _configuration.GetConnectionString("DBCS");

            using SqlConnection con = new SqlConnection(cs);
            con.Open();

            SqlCommand cmd = new SqlCommand(
                "SELECT Id, Name FROM UserSignup WHERE Email = @e AND Password = @p", con);

            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", password);

            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                HttpContext.Session.SetInt32("UserId", (int)reader["Id"]);
                HttpContext.Session.SetString("UserName", reader["Name"].ToString());

                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Invalid email or password." });
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
