using Microsoft.AspNetCore.Mvc;

namespace NUTRIBITE.Controllers
{
    public class VendorController : Controller
    {
        public IActionResult Index()
        {
            return Content("Vendor controller is working");
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

       public IActionResult Dashboard()
        {
            return View();
        }
    }
}
