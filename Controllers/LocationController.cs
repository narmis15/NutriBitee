using Microsoft.AspNetCore.Mvc;

namespace NUTRIBITE.Controllers
{
    public class LocationController : Controller
    {
        // GET /Location  (also accessible via /location)
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}