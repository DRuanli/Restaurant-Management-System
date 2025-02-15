using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Data;
using RestaurantSystem.Models;
using RestaurantSystem.Enums;

namespace RestaurantSystem.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(ApplicationDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult UserDashboard()
        {
            if (HttpContext.Session.GetString("UserRole") != "user")
            {
                return RedirectToAction("Welcome", "Account");
            }
            return View();
        }

        public async Task<IActionResult> Reservations()
        {
            if (HttpContext.Session.GetString("UserRole") != "user")
            {
                return RedirectToAction("Welcome", "Account");
            }

            try
            {
                var userId = GetCurrentUserId();
                var reservations = await _context.Reservations
                    .OrderByDescending(r => r.ReservationDate)
                    .ThenBy(r => r.ReservationTime)
                    .ToListAsync();

                ViewBag.IsAdmin = false;
                return View("~/Views/Reservations/Index.cshtml", reservations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservations");
                TempData["ErrorMessage"] = "Error loading reservations";
                return View("~/Views/Reservations/Index.cshtml", new List<Reservation>());
            }
        }


        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("UserRole") != "user")
            {
                return RedirectToAction("Welcome", "Account");
            }

            var reservation = new Reservation
            {
                ReservationDate = DateTime.Today,
                ReservationTime = DateTime.Now.TimeOfDay,
                Status = "Pending"
            };

            return View("~/Views/Reservations/Create.cshtml", reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation reservation)
        {
            if (HttpContext.Session.GetString("UserRole") != "user")
            {
                return RedirectToAction("Welcome", "Account");
            }
            try
            {
                if (ModelState.IsValid)
                {
                    // Set default values
                    reservation.Status = "Pending";
                    reservation.TableNumber = await AssignTableNumber(reservation);
                    reservation.SpecialRequests = string.IsNullOrEmpty(reservation.SpecialRequests) ? "None" : reservation.SpecialRequests;
                    reservation.Notes = string.IsNullOrEmpty(reservation.Notes) ? "None" : reservation.Notes;

                    _context.Reservations.Add(reservation);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Reservation created successfully!";
                    return RedirectToAction(nameof(Reservations));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating reservation: {ex.Message}");
                ModelState.AddModelError("", $"Database error: {ex.Message}");
            }
            ViewBag.Role = "user";
            return View("~/Views/Reservations/Create.cshtml", reservation);
        }

        private async Task<bool> CheckTableAvailability(DateTime date, TimeSpan time)
        {
            // Get existing reservations for the date
            var existingReservations = await _context.Reservations
                .Where(r => r.ReservationDate.Date == date.Date
                        && r.Status != "Cancelled")
                .ToListAsync(); // Get the data first

            // Then count matching reservations in memory
            var reservationCount = existingReservations
                .Count(r => Math.Abs((r.ReservationTime - time).TotalMinutes) < 120);

            const int MAX_TABLES = 40; // Maximum capacity
            return reservationCount < MAX_TABLES;
        }

        private async Task<int> AssignTableNumber(Reservation newReservation)
        {
            try
            {
                // Get all existing reservations for the same date
                var existingTables = await _context.Reservations
                    .Where(r => r.ReservationDate.Date == newReservation.ReservationDate.Date
                            && r.Status != "Cancelled")
                    .ToListAsync(); // Get the data first

                // Then do the time comparison in memory
                var usedTables = existingTables
                    .Where(r => Math.Abs((r.ReservationTime - newReservation.ReservationTime).TotalMinutes) < 120)
                    .Select(r => r.TableNumber)
                    .ToList(); // Regular ToList() since we're working with in-memory data

                // Find the first available table number
                int tableNumber = 1;
                while (usedTables.Contains(tableNumber))
                {
                    tableNumber++;
                }

                return tableNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning table number");
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "user")
            {
                return RedirectToAction("Welcome", "Account");
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            ViewBag.Role = "user"; // Important for navigation
            return View("~/Views/Reservations/Details.cshtml", reservation);
        }


        public async Task<IActionResult> Orders()
        {
            if (HttpContext.Session.GetString("UserRole") != "user")
            {
                return RedirectToAction("Welcome", "Account");
            }

            try
            {
                var menuItems = await _context.MenuItems
                    .Where(m => m.IsAvailable)
                    .OrderBy(m => m.Category)
                    .ToListAsync();

                ViewBag.Categories = await _context.MenuItems
                    .Select(m => m.Category)
                    .Distinct()
                    .ToListAsync();

                var currentOrder = await GetOrCreateCurrentOrder();
                ViewBag.CurrentOrderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == currentOrder.Id)
                    .Include(oi => oi.MenuItem)
                    .ToListAsync();

                ViewBag.IsAdmin = false;
                return View("~/Views/Orders/Index.cshtml", menuItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders view");
                return View("~/Views/Orders/Index.cshtml", new List<MenuItem>());
            }
        }

        private async Task<Order> GetOrCreateCurrentOrder()
        {
            var userId = GetCurrentUserId();
            var currentOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Status == OrderStatus.Draft && o.UserId == userId);

            if (currentOrder == null)
            {
                currentOrder = new Order
                {
                    OrderNumber = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Draft,
                    UserId = userId,
                    NumberOfGuests = 1
                };
                _context.Orders.Add(currentOrder);
                await _context.SaveChangesAsync();
            }

            return currentOrder;
        }

        private int? GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdString, out int userId) ? userId : null;
        }
    }
}