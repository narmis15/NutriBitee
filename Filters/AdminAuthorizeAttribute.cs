using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NutriBite.Filters
{
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var admin = context.HttpContext.Session.GetString("Admin");

            if (string.IsNullOrEmpty(admin))
            {
                context.Result = new RedirectToActionResult(
                    "Login", "Admin", null);
            }
        }
    }
}



