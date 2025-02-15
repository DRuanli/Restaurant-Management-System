using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Data;
using RestaurantSystem.Models;

namespace RestaurantSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }


        // Welcome Page
        public IActionResult Welcome()
        {
            // Clear any existing role data when visiting welcome page
            HttpContext.Session.Clear();
            TempData["Role"] = null;
            ViewBag.Role = null;

            return View();
        }

        // Login Page
        public IActionResult Login(string role)
        {
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Welcome");
            }

            ViewBag.Role = role;
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model, string role)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Please fill out all fields.";
                ViewBag.Role = role;
                return View();
            }

            try
            {
                // Authenticate user against the database
                var user = _context.Users
                    .FirstOrDefault(u => u.Username == model.Username &&
                                       u.Password == model.Password &&
                                       u.Role == role);

                if (user != null)
                {
                    _logger.LogInformation($"User {user.Username} logged in successfully with role {role}");

                    // Store both role and userId in session
                    HttpContext.Session.SetString("UserRole", role);
                    HttpContext.Session.SetString("UserId", user.Id.ToString());  // Add this line
                    TempData["Role"] = role;
                    ViewBag.Role = role;

                    // Redirect based on role
                    if (role == "admin")
                    {
                        return RedirectToAction("AdminDashboard", "Admin");
                    }
                    else if (role == "user")
                    {
                        return RedirectToAction("UserDashboard", "User");
                    }
                }

                _logger.LogWarning($"Failed login attempt for username {model.Username} with role {role}");
                ViewBag.ErrorMessage = "Invalid username or password. Please try again.";
                ViewBag.Role = role;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process");
                ViewBag.ErrorMessage = "An error occurred during login. Please try again.";
                ViewBag.Role = role;
                return View();
            }
        }

        // Logout functionality
        public IActionResult Logout()
        {
            var username = User.Identity?.Name;
            _logger.LogInformation($"User {username} logged out");

            // Clear all session and authentication data
            HttpContext.Session.Clear();
            TempData.Clear();

            TempData["Message"] = "You have successfully logged out.";
            return RedirectToAction("Welcome");
        }

        // Add Register GET action
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if username already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Username already exists");
                    return View(model);
                }

                // Create new user
                var user = new User
                {
                    Username = model.Username,
                    Password = model.Password, // In production, hash this password
                    Role = "user" // Default role
                };

                try
                {
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Auto-login after registration
                    HttpContext.Session.SetString("UserRole", "user");
                    HttpContext.Session.SetString("UserId", user.Id.ToString());

                    return RedirectToAction("UserDashboard", "User");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during registration");
                    ModelState.AddModelError("", "Error creating account");
                }
            }
            return View(model);
        }
    }
}