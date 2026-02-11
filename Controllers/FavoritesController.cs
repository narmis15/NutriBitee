using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace NUTRIBITE.Controllers
{
    public class FavoritesController : Controller
    {
        private readonly IConfiguration _configuration;
        public FavoritesController(IConfiguration configuration) => _configuration = configuration;

        // GET /Favorites/UserFavorites
        // Returns { authenticated: true/false, ids: [1,2,3] }
        [HttpGet]
        public IActionResult UserFavorites()
        {
            var uid = HttpContext.Session.GetInt32("UserId");
            if (!uid.HasValue) return Json(new { authenticated = false, ids = new int[] { } });

            var cs = _configuration.GetConnectionString("DBCS") ?? throw new Exception("DBCS not found");
            var ids = new List<int>();
            using var con = new SqlConnection(cs);
            con.Open();
            using var cmd = new SqlCommand("SELECT FoodId FROM Favorites WHERE UserId = @u", con);
            cmd.Parameters.AddWithValue("@u", uid.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                if (r["FoodId"] != null && int.TryParse(r["FoodId"].ToString(), out var fid))
                    ids.Add(fid);
            }

            return Json(new { authenticated = true, ids });
        }

        // POST /Favorites/Toggle
        // body: foodId
        // Returns { success: true, favorited: true/false }
        [HttpPost]
        public IActionResult Toggle(int foodId)
        {
            var uid = HttpContext.Session.GetInt32("UserId");
            if (!uid.HasValue) return Json(new { success = false, authenticated = false, message = "Not authenticated" });

            var cs = _configuration.GetConnectionString("DBCS") ?? throw new Exception("DBCS not found");
            try
            {
                using var con = new SqlConnection(cs);
                con.Open();

                // check existing
                using var chk = new SqlCommand("SELECT TOP(1) Id FROM Favorites WHERE UserId = @u AND FoodId = @f", con);
                chk.Parameters.AddWithValue("@u", uid.Value);
                chk.Parameters.AddWithValue("@f", foodId);
                var existing = chk.ExecuteScalar();

                if (existing != null)
                {
                    // remove (toggle off)
                    using var del = new SqlCommand("DELETE FROM Favorites WHERE Id = @id", con);
                    del.Parameters.AddWithValue("@id", Convert.ToInt32(existing));
                    del.ExecuteNonQuery();
                    return Json(new { success = true, favorited = false });
                }
                else
                {
                    // insert (prevent duplicates by re-checking constraints)
                    using var ins = new SqlCommand("INSERT INTO Favorites (UserId, FoodId, CreatedAt) VALUES (@u, @f, GETDATE())", con);
                    ins.Parameters.AddWithValue("@u", uid.Value);
                    ins.Parameters.AddWithValue("@f", foodId);
                    ins.ExecuteNonQuery();
                    return Json(new { success = true, favorited = true });
                }
            }
            catch (Exception ex)
            {
                // do not leak sensitive details
                return Json(new { success = false, message = "Server error" });
            }
        }
    }
}