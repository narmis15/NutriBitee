using Microsoft.AspNetCore.Mvc;

namespace NUTRIBITE.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
