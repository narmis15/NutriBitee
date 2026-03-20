using Microsoft.AspNetCore.Mvc;
using System.Linq;
using NUTRIBITE.Models;

namespace NUTRIBITE.Controllers
{
    public class ElderlyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ElderlyController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var foods = _context.Foods
                .Where(f => f.Status == "Active")
                .ToList();

            return View(foods);
        }
    }
}
