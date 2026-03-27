using Microsoft.AspNetCore.Mvc;
using global::NUTRIBITE.Services;
using global::NUTRIBITE.Filters;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace NUTRIBITE.Controllers
{
    [AdminAuthorize]
    public class AdminOrdersController : Controller
    {
        private readonly IOrderService _orderService;
        public AdminOrdersController(IOrderService orderService) => _orderService = orderService;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptOrderAndRedirect(int orderId)
        {
            var ok = await _orderService.UpdateOrderStatusAsync(orderId, "Accepted");
            if (!ok)
            {
                TempData["Error"] = "Unable to accept order.";
                return RedirectToAction("OrderManagement", "Admin");
            }
            TempData["Success"] = "Order accepted.";
            return RedirectToAction("OrderManagement", "Admin");
        }

        public async Task<IActionResult> OrderDetails(int orderId)
        {
            // This method is added to accommodate the requested LINQ projection logic
            // in the context of the AdminOrdersController.
            var order = await _orderService.GetOrderQueryable()
                .Where(o => o.OrderId == orderId)
                .Select(o => new
                {
                    OrderDateTime = o.CreatedAt.HasValue ? o.CreatedAt.Value.ToString("dd MMM yyyy HH:mm") : string.Empty,
                    o.TotalAmount,
                    o.CommissionAmount,
                    o.VendorAmount
                }).FirstOrDefaultAsync();

            if (order == null) return NotFound();
            return View(order);
        }
    }
}