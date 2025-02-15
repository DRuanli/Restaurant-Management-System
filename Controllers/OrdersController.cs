using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Data;
using RestaurantSystem.Models;
using RestaurantSystem.Enums;

namespace RestaurantSystem.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(ApplicationDbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Role = HttpContext.Session.GetString("UserRole");
            ViewBag.IsAdmin = ViewBag.Role == "admin";
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

            return View(menuItems);
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentOrder()
        {
            var currentOrder = await GetOrCreateCurrentOrder();
            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == currentOrder.Id)
                .Include(oi => oi.MenuItem)
                .ToListAsync();

            return PartialView("_CurrentOrder", orderItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToOrder(int menuItemId, int quantity, string notes)
        {
            try
            {
                var currentOrder = await GetOrCreateCurrentOrder();
                var menuItem = await _context.MenuItems.FindAsync(menuItemId);

                if (menuItem == null)
                    return Json(new { success = false, message = "Menu item not found" });

                var orderItem = new OrderItem
                {
                    OrderId = currentOrder.Id,
                    MenuItemId = menuItemId,
                    Quantity = quantity,
                    UnitPrice = menuItem.Price,
                    Notes = notes
                };

                _context.OrderItems.Add(orderItem);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to order");
                return Json(new { success = false, message = "Error adding item to order" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitOrder()
        {
            try
            {
                var order = await GetOrCreateCurrentOrder();

                if (!order.OrderItems.Any())
                {
                    return Json(new { success = false, message = "Cannot submit empty order" });
                }

                await _context.Entry(order)
                    .Collection(o => o.OrderItems)
                    .Query()
                    .Include(oi => oi.MenuItem)
                    .LoadAsync();

                order.Subtotal = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);
                order.Tax = decimal.Round(order.Subtotal * 0.08M, 2);
                order.Total = order.Subtotal + order.Tax;
                order.Status = OrderStatus.Pending;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("Payment", "Orders", new { id = order.Id })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting order: {Message}", ex.Message);
                return Json(new { success = false, message = "Error processing order. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOrderItem(int orderItemId)
        {
            try
            {
                var orderItem = await _context.OrderItems.FindAsync(orderItemId);
                if (orderItem == null)
                    return Json(new { success = false, message = "Item not found" });

                _context.OrderItems.Remove(orderItem);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order item");
                return Json(new { success = false, message = "Error deleting item" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                order.Status = OrderStatus.Completed;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing order");
                return Json(new { success = false, message = "Error completing order" });
            }
        }

        private async Task<Order> GetOrCreateCurrentOrder()
        {
            var currentOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Status == OrderStatus.Draft);

            if (currentOrder == null)
            {
                currentOrder = new Order
                {
                    OrderNumber = GenerateOrderNumber(),
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Draft,
                    NumberOfGuests = 1,
                    Subtotal = 0,
                    Tax = 0,
                    Total = 0
                };
                _context.Orders.Add(currentOrder);
                await _context.SaveChangesAsync();
            }

            return currentOrder;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int orderItemId, int change)
        {
            try
            {
                var orderItem = await _context.OrderItems.FindAsync(orderItemId);
                if (orderItem == null)
                    return Json(new { success = false, message = "Item not found" });

                orderItem.Quantity = Math.Max(1, orderItem.Quantity + change);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity");
                return Json(new { success = false, message = "Error updating quantity" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveDraft()
        {
            try
            {
                var currentOrder = await GetOrCreateCurrentOrder();
                currentOrder.Status = OrderStatus.Draft;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving draft");
                return Json(new { success = false, message = "Error saving draft" });
            }
        }

        [HttpGet]
        [Route("Orders/Payment/{id}")]
        public async Task<IActionResult> Payment(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return RedirectToAction("Index");

            if (order.Total == 0)
            {
                order.Subtotal = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);
                order.Tax = decimal.Round(order.Subtotal * 0.08M, 2);
                order.Total = order.Subtotal + order.Tax;
                await _context.SaveChangesAsync();
            }

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderHistory()
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "User not logged in" });

                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                    .Where(o => o.UserId == int.Parse(userId) &&
                               o.Status == OrderStatus.Completed)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return PartialView("_OrderHistory", orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order history");
                return Json(new { success = false, message = "Error loading order history" });
            }
        }

        [HttpPost]
        [Route("Orders/ProcessPayment")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            try
            {
                var order = await _context.Orders.FindAsync(request.OrderId);
                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                bool paymentSuccess = false;

                switch (request.PaymentMethod.ToLower())
                {
                    case "card":
                        paymentSuccess = await ProcessCardPayment(request);
                        break;
                    case "qr":
                        paymentSuccess = await ProcessQRPayment(request);
                        break;
                    case "cash":
                        paymentSuccess = await ProcessCashPayment(request);
                        break;
                    default:
                        return Json(new { success = false, message = "Invalid payment method" });
                }

                if (paymentSuccess)
                {
                    order.Status = OrderStatus.Completed;
                    order.UserId = HttpContext.Session.GetString("UserId") != null ?
                        int.Parse(HttpContext.Session.GetString("UserId")) : null;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Payment processing failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                return Json(new { success = false, message = "Error processing payment" });
            }
        }

        private async Task<bool> ProcessCardPayment(PaymentRequest request)
        {
            await Task.Delay(2000);
            return true;
        }

        private async Task<bool> ProcessQRPayment(PaymentRequest request)
        {
            await Task.Delay(2000);
            return true;
        }

        private async Task<bool> ProcessCashPayment(PaymentRequest request)
        {
            if (request.AmountReceived < request.Amount)
                return false;

            await Task.Delay(1000);
            return true;
        }

        public class PaymentRequest
        {
            public int OrderId { get; set; }
            public string PaymentMethod { get; set; }
            public decimal Amount { get; set; }
            public decimal AmountReceived { get; set; }
            public string CardNumber { get; set; }
            public string ExpiryDate { get; set; }
            public string CVV { get; set; }
        }

        private string GenerateOrderNumber()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }
    }
}