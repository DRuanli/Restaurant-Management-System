using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Data;
using RestaurantSystem.Models;
using Microsoft.Extensions.Logging;
namespace RestaurantSystem.Controllers
{
    public class AdminController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<AdminController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }
        // Admin Dashboard
        public IActionResult AdminDashboard()
        {
            // Check if the user is authorized as admin
            if (HttpContext.Session.GetString("UserRole") != "admin")
            {
                return RedirectToAction("Welcome", "Account");
            }
            ViewBag.Role = "admin";
            return View();
        }



        [HttpGet]
        public async Task<IActionResult> Menu()
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Welcome", "Account");

            ViewBag.Role = "admin";
            var menuItems = await _context.MenuItems.OrderBy(m => m.Category).ToListAsync();
            return View(menuItems);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAvailability(int id, bool isAvailable)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null) return NotFound();

            menuItem.IsAvailable = isAvailable;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuItem(int id)
        {
            try
            {
                var menuItem = await _context.MenuItems.FindAsync(id);
                if (menuItem == null) return NotFound();
                return Json(menuItem);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting menu item: {ex.Message}");
                return StatusCode(500, "Error retrieving menu item");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMenuItem(int id, [FromForm] MenuItem menuItem, IFormFile? image)
        {
            try
            {
                var existingItem = await _context.MenuItems.FindAsync(id);
                if (existingItem == null) return NotFound();

                // Handle image upload
                if (image != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    var filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);

                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    // Save new image
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingItem.ImageUrl))
                    {
                        var oldFilePath = Path.Combine(_env.WebRootPath, existingItem.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    existingItem.ImageUrl = $"/uploads/{fileName}";
                }

                // Update other properties
                existingItem.Name = menuItem.Name;
                existingItem.Description = menuItem.Description;
                existingItem.Price = menuItem.Price;
                existingItem.Category = menuItem.Category;
                existingItem.IsAvailable = menuItem.IsAvailable;

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating menu item: {ex.Message}");
                return StatusCode(500, "Error updating menu item");
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            try
            {
                var menuItem = await _context.MenuItems.FindAsync(id);
                if (menuItem == null) return NotFound();

                _context.MenuItems.Remove(menuItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting menu item: {ex.Message}");
                return StatusCode(500, "Error deleting menu item");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveMenuItem([FromForm] MenuItem menuItem, IFormFile? image)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(menuItem.Name) || menuItem.Price <= 0)
                {
                    return Json(new { success = false, message = "Name and price are required" });
                }

                // Handle image upload
                if (image != null)
                {
                    // Validate image size (5MB limit)
                    if (image.Length > 5 * 1024 * 1024)
                    {
                        return Json(new { success = false, message = "Image size must be less than 5MB" });
                    }

                    // Validate image type
                    var allowedTypes = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                    if (!allowedTypes.Contains(extension))
                    {
                        return Json(new { success = false, message = "Only JPG and PNG images are allowed" });
                    }

                    try
                    {
                        // Create uploads directory if it doesn't exist
                        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Generate unique filename
                        var fileName = $"{Guid.NewGuid()}{extension}";
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        // Save new image
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }

                        menuItem.ImageUrl = $"/uploads/{fileName}";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error saving image: {ex.Message}");
                        return Json(new { success = false, message = "Error saving image" });
                    }
                }

                // Save or update menu item
                if (menuItem.Id == 0)
                {
                    menuItem.IsAvailable = true;  // Set default availability
                    _context.MenuItems.Add(menuItem);
                }
                else
                {
                    // Update existing item
                    var existing = await _context.MenuItems.FindAsync(menuItem.Id);
                    if (existing == null)
                    {
                        return Json(new { success = false, message = "Menu item not found" });
                    }

                    // If there's a new image, delete the old one
                    if (image != null && !string.IsNullOrEmpty(existing.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(_env.WebRootPath, existing.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Error deleting old image: {ex.Message}");
                            }
                        }
                    }

                    // Update properties
                    _context.Entry(existing).CurrentValues.SetValues(menuItem);
                    if (!string.IsNullOrEmpty(menuItem.ImageUrl))
                    {
                        existing.ImageUrl = menuItem.ImageUrl;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Menu item saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving menu item: {ex.Message}");
                return Json(new { success = false, message = "Error saving menu item" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory([FromBody] string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return Json(new { success = false, message = "Category name cannot be empty" });
            }

            try
            {
                // Check if the category already exists
                var existingCategory = await _context.MenuItems
                    .Where(m => m.Category == categoryName)
                    .FirstOrDefaultAsync();

                if (existingCategory != null)
                {
                    return Json(new { success = false, message = "Category already exists" });
                }

                // Add a dummy item with the new category (if your system requires it)
                var newMenuItem = new MenuItem
                {
                    Name = "New Item",
                    Category = categoryName,
                    Price = 0, // Set a default price
                    IsAvailable = false,
                    Description = "Newly added category"
                };

                _context.MenuItems.Add(newMenuItem);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Category added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding category: {ex.Message}");
                return Json(new { success = false, message = "Error adding category" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory([FromBody] string name)
        {
            var itemsInCategory = await _context.MenuItems
                .Where(m => m.Category == name)
                .ToListAsync();

            if (itemsInCategory.Any())
                return Json(new { success = false, message = "Cannot delete category with existing items" });

            return Json(new { success = true });
        }

        // Staff Management
        public async Task<IActionResult> Staff()
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Welcome", "Account");
            var staff = await _context.Staff.ToListAsync();
            return View(staff);
        }


        // Reports
        public IActionResult Report()
        {
            // Check if the user is authorized as admin
            if (TempData["Role"] as string != "admin")
            {
                return RedirectToAction("Welcome", "Account");
            }
            ViewBag.Role = "admin";
            return View();
        }
    }
}
