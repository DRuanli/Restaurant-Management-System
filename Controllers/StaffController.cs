using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Data;
using RestaurantSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace RestaurantSystem.Controllers
{
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StaffController> _logger;
        private readonly IWebHostEnvironment _env;

        public StaffController(ApplicationDbContext context, ILogger<StaffController> logger, IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserRole") != "admin")
                return RedirectToAction("Welcome", "Account");

            var staff = await _context.Staff.ToListAsync();
            return View(staff);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStaff()
        {
            try
            {
                var staff = await _context.Staff.ToListAsync();
                return Json(staff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching staff list");
                return Json(new { success = false, message = "Error fetching staff list" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddStaff([FromForm] Staff staff, IFormFile? image)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid staff data" });
                }

                staff.Status = "Active";

                if (image != null)
                {
                    if (image.Length > 5 * 1024 * 1024) // 5MB limit
                    {
                        return Json(new { success = false, message = "Image file size exceeds 5MB limit" });
                    }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(image.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return Json(new { success = false, message = "Only JPG and PNG formats are allowed" });
                    }

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "staff");
                    Directory.CreateDirectory(uploadPath);
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    staff.ImageUrl = $"/uploads/staff/{fileName}";
                }

                _context.Staff.Add(staff);
                await _context.SaveChangesAsync();

                return Json(new { success = true, staff });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding staff member");
                return Json(new { success = false, message = "Error adding staff member" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditStaff(int id, [FromForm] Staff staff, IFormFile? image)
        {
            try
            {
                var existingStaff = await _context.Staff.FindAsync(id);
                if (existingStaff == null)
                {
                    return Json(new { success = false, message = "Staff member not found" });
                }

                if (image != null)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingStaff.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(_env.WebRootPath, existingStaff.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Save new image
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "staff");
                    Directory.CreateDirectory(uploadPath);
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    existingStaff.ImageUrl = $"/uploads/staff/{fileName}";
                }

                // Update other properties
                existingStaff.Name = staff.Name;
                existingStaff.Email = staff.Email;
                existingStaff.Phone = staff.Phone;
                existingStaff.Position = staff.Position;
                existingStaff.Department = staff.Department;

                await _context.SaveChangesAsync();
                return Json(new { success = true, staff = existingStaff });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff member");
                return Json(new { success = false, message = "Error updating staff member" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStaff(int id)
        {
            try
            {
                var staff = await _context.Staff.FindAsync(id);
                if (staff == null)
                {
                    return Json(new { success = false, message = "Staff member not found" });
                }
                return Json(new { success = true, staff });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching staff member");
                return Json(new { success = false, message = "Error fetching staff member" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            try
            {
                var staff = await _context.Staff.FindAsync(id);
                if (staff == null)
                {
                    return Json(new { success = false, message = "Staff member not found" });
                }

                // Delete associated image if exists
                if (!string.IsNullOrEmpty(staff.ImageUrl))
                {
                    var imagePath = Path.Combine(_env.WebRootPath, staff.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Staff.Remove(staff);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staff member");
                return Json(new { success = false, message = "Error deleting staff member" });
            }
        }
    }
}