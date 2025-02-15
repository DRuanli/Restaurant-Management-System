// Controllers/FeedbackController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Data;
using RestaurantSystem.Models;

namespace RestaurantSystem.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(ApplicationDbContext context, ILogger<FeedbackController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Feedback/Create
        public async Task<IActionResult> Create(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.OrderItems = order.OrderItems;
            ViewBag.OrderId = orderId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Feedback feedback, [FromForm] List<DishFeedback> dishFeedbacks)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(userId))
                {
                    feedback.UserId = int.Parse(userId);
                }

                feedback.CreatedAt = DateTime.UtcNow;
                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                foreach (var dishFeedback in dishFeedbacks)
                {
                    dishFeedback.FeedbackId = feedback.Id;
                    _context.DishFeedbacks.Add(dishFeedback);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thank you for your feedback!";
                return RedirectToAction("UserDashboard", "User");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting feedback");
                ModelState.AddModelError("", "Error submitting feedback. Please try again.");
                return View(feedback);
            }
        }
    }
}