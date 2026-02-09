using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NutriBite.Filters
{
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // ✅ Login action ko allow karo
            var action = context.ActionDescriptor.RouteValues["action"];
            var controller = context.ActionDescriptor.RouteValues["controller"];

            if (controller == "Admin" && action == "Login")
            {
                return;
            }

            var admin = context.HttpContext.Session.GetString("Admin");

            if (string.IsNullOrEmpty(admin))
            {
                context.Result = new RedirectToActionResult("Login", "Admin", null);
            }
        }
    }
}





