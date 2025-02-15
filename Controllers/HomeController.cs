using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Models;
using System.Diagnostics;

namespace RestaurantSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Get user role from session
            var role = HttpContext.Session.GetString("UserRole");

            // Redirect based on role
            if (role == "admin")
            {
                return RedirectToAction("AdminDashboard", "Admin");
            }
            else if (role == "user")
            {
                return RedirectToAction("UserDashboard", "User");
            }
            else
            {
                // If no role (not logged in), redirect to Welcome page
                return RedirectToAction("Welcome", "Account");
            }
        }

        // Keep other actions like Privacy and Error
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
