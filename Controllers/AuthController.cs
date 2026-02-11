using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace NUTRIBITE.Controllers
{
    public partial class AuthController : Controller
    {
        private readonly IConfiguration _configuration;
        public AuthController(IConfiguration configuration) => _configuration = configuration;

        // GET /Auth/Login  (renders login page)
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // GET /Auth/Register  (renders signup page)
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Lightweight probe used by client UI
        [HttpGet]
        public IActionResult IsAuthenticated()
        {
            return Json(new { authenticated = HttpContext.Session.GetInt32("UserId").HasValue,
                              userName = HttpContext.Session.GetString("UserName") ?? "" });
        }

        // POST /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return Json(new { success = false, message = "Email and password are required." });

            string cs = _configuration.GetConnectionString("DBCS") ?? throw new Exception("DBCS not found");
            using var con = new SqlConnection(cs);
            con.Open();

            // try to find user by email
            using var cmd = new SqlCommand("SELECT TOP(1) * FROM [Users] WHERE Email = @e", con);
            cmd.Parameters.AddWithValue("@e", email.Trim());
            using var r = cmd.ExecuteReader();
            if (!r.Read())
                return Json(new { success = false, message = "Invalid credentials." });

            // read common fields if present
            var id = r["Id"] as object;
            var userId = id != null ? Convert.ToInt32(id) : 0;
            var name = r["Name"] as string ?? r["FullName"] as string ?? r["UserName"] as string ?? (r["Email"] as string ?? "");
            string dbPassword = r["Password"] as string ?? r["PasswordHash"] as string;
            string dbSalt = null;
            if (r.GetSchemaTable().Columns.Contains("PasswordSalt"))
                dbSalt = r["PasswordSalt"] as string;
            else if (r.GetSchemaTable().Columns.Contains("Salt"))
                dbSalt = r["Salt"] as string;

            // Prefer PBKDF2 if salt is present
            if (!string.IsNullOrEmpty(dbSalt) && !string.IsNullOrEmpty(dbPassword))
            {
                try
                {
                    var saltBytes = Convert.FromBase64String(dbSalt);
                    var hashBytes = Convert.FromBase64String(dbPassword);
                    using var derive = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
                    var testHash = derive.GetBytes(hashBytes.Length);
                    if (!testHash.SequenceEqual(hashBytes))
                        return Json(new { success = false, message = "Invalid credentials." });
                }
                catch
                {
                    // fall through to plain compare below
                    if (!string.Equals(dbPassword, password))
                        return Json(new { success = false, message = "Invalid credentials." });
                }
            }
            else if (!string.IsNullOrEmpty(dbPassword))
            {
                // fallback - plain compare (best-effort)
                if (!string.Equals(dbPassword, password))
                    return Json(new { success = false, message = "Invalid credentials." });
            }
            else
            {
                return Json(new { success = false, message = "User has no valid password stored." });
            }

            // success — store minimal session
            HttpContext.Session.SetInt32("UserId", userId);
            HttpContext.Session.SetString("UserName", name ?? email);
            return Json(new { success = true, userName = name ?? email });
        }

        // POST /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string name, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return Json(new { success = false, message = "Email and password are required." });

            // generate salt + pbkdf2 hash
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            using var derive = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = derive.GetBytes(32);

            string cs = _configuration.GetConnectionString("DBCS") ?? throw new Exception("DBCS not found");
            using var con = new SqlConnection(cs);
            con.Open();

            // Attempt insert into Users table using common columns. If your Users table has different required columns,
            // this INSERT may fail. If so, share schema and I'll adapt the SQL.
            using var cmd = new SqlCommand(@"
IF EXISTS (SELECT 1 FROM [Users] WHERE Email = @e)
    SELECT -1
ELSE
BEGIN
    INSERT INTO [Users] (Name, Email, PasswordHash, PasswordSalt)
    VALUES (@n, @e, @ph, @ps);
    SELECT SCOPE_IDENTITY();
END", con);
            cmd.Parameters.AddWithValue("@n", (object)name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@e", email.Trim());
            cmd.Parameters.AddWithValue("@ph", Convert.ToBase64String(hash));
            cmd.Parameters.AddWithValue("@ps", Convert.ToBase64String(salt));

            var result = cmd.ExecuteScalar();
            if (result is int rInt && rInt == -1)
                return Json(new { success = false, message = "Email already registered." });

            // log in the user
            int newId = Convert.ToInt32(result);
            HttpContext.Session.SetInt32("UserId", newId);
            HttpContext.Session.SetString("UserName", name ?? email);
            return Json(new { success = true, userName = name ?? email });
        }

        // GET /Auth/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("UserName");
            return RedirectToAction("Index", "Home");
        }

        // GET: /Auth/Forgot
        [HttpGet]
        public IActionResult Forgot()
        {
            return View();
        }

        // POST: /Auth/Forgot
        // This action only triggers a generic "email sent" response to avoid disclosing account existence.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Forgot(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Please provide an email address." });

            // Attempt to enqueue/send reset email if your system supports it.
            // For safety we don't leak whether the email exists; always return success message.
            try
            {
                // TODO: hook real email sending logic here.
                return Json(new { success = true, message = "If an account exists for this email, password reset instructions have been sent." });
            }
            catch
            {
                return Json(new { success = false, message = "Unable to process request. Try again later." });
            }
        }
    }
}