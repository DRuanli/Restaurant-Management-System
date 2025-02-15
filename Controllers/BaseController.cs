using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RestaurantSystem.Controllers
{
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Get role from Session or TempData
            var role = HttpContext.Session.GetString("UserRole") ?? TempData["Role"]?.ToString();

            // Set ViewBag.Role for the layout
            ViewBag.Role = role;

            // Preserve TempData["Role"]
            if (TempData["Role"] != null)
            {
                TempData.Keep("Role");
            }
        }
    }
}