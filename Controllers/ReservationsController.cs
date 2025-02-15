using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Data;
using RestaurantSystem.Models;
using System.Data;

namespace RestaurantSystem.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReservationsController> _logger;
        private bool IsAdmin => HttpContext.Session.GetString("UserRole") == "admin";

        public ReservationsController(ApplicationDbContext context, ILogger<ReservationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private IActionResult RedirectToDashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "admin")
                return RedirectToAction("AdminDashboard", "Admin");
            else if (role == "user")
                return RedirectToAction("UserDashboard", "User");
            else
                return RedirectToAction("Welcome", "Account");
        }

        public async Task<IActionResult> Index()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole))
            {
                return RedirectToDashboard();
            }

            ViewBag.Role = userRole;
            ViewBag.IsAdmin = userRole == "admin";

            try
            {
                var sql = @"SELECT * FROM dbo.Reservations 
                           ORDER BY ReservationDate DESC, ReservationTime";
                var reservations = await _context.Reservations
                    .FromSqlRaw(sql)
                    .ToListAsync();
                return View(reservations);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving reservations: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading reservations.";
                return View(new List<Reservation>());
            }
        }

        public IActionResult Create()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole))
            {
                return RedirectToDashboard();
            }

            ViewBag.Role = userRole;
            ViewBag.IsAdmin = userRole == "admin";

            var reservation = new Reservation
            {
                ReservationDate = DateTime.Today,
                ReservationTime = DateTime.Now.TimeOfDay,
                Status = "Pending"
            };
            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation reservation)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole))
            {
                return RedirectToDashboard();
            }

            ViewBag.Role = userRole;
            ViewBag.IsAdmin = userRole == "admin";

            try
            {
                if (ModelState.IsValid)
                {
                    reservation.Status = "Pending";
                    reservation.TableNumber = await AssignTableNumber(reservation);
                    reservation.SpecialRequests = string.IsNullOrEmpty(reservation.SpecialRequests) ? "None" : reservation.SpecialRequests;
                    reservation.Notes = string.IsNullOrEmpty(reservation.Notes) ? "None" : reservation.Notes;

                    var sql = @"INSERT INTO dbo.Reservations 
                            (CustomerName, PhoneNumber, Email, NumberOfGuests, 
                             ReservationDate, ReservationTime, TableNumber, 
                             Status, SpecialRequests, Notes)
                            VALUES 
                            (@CustomerName, @PhoneNumber, @Email, @NumberOfGuests,
                             @ReservationDate, @ReservationTime, @TableNumber,
                             @Status, @SpecialRequests, @Notes)";

                    using (var command = _context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = sql;

                        var parameters = new[]
                        {
                            CreateParameter(command, "@CustomerName", reservation.CustomerName),
                            CreateParameter(command, "@PhoneNumber", reservation.PhoneNumber),
                            CreateParameter(command, "@Email", reservation.Email),
                            CreateParameter(command, "@NumberOfGuests", reservation.NumberOfGuests),
                            CreateParameter(command, "@ReservationDate", reservation.ReservationDate),
                            CreateParameter(command, "@ReservationTime", reservation.ReservationTime),
                            CreateParameter(command, "@TableNumber", reservation.TableNumber),
                            CreateParameter(command, "@Status", reservation.Status),
                            CreateParameter(command, "@SpecialRequests", reservation.SpecialRequests),
                            CreateParameter(command, "@Notes", reservation.Notes)
                        };

                        command.Parameters.AddRange(parameters);

                        if (command.Connection.State != ConnectionState.Open)
                        {
                            await command.Connection.OpenAsync();
                        }

                        await command.ExecuteNonQueryAsync();
                    }

                    TempData["SuccessMessage"] = "Reservation created successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating reservation: {ex.Message}");
                ModelState.AddModelError("", $"Database error: {ex.Message}");
            }

            return View(reservation);
        }

        public async Task<IActionResult> Details(int? id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole))
            {
                return RedirectToDashboard();
            }

            ViewBag.Role = userRole;
            ViewBag.IsAdmin = userRole == "admin";

            if (id == null)
            {
                return NotFound();
            }

            var sql = "SELECT * FROM dbo.Reservations WHERE Id = @Id";
            var parameter = new Microsoft.Data.SqlClient.SqlParameter("@Id", id);

            var reservation = await _context.Reservations
                .FromSqlRaw(sql, parameter)
                .FirstOrDefaultAsync();

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole))
            {
                return RedirectToDashboard();
            }

            if (!IsAdmin)
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var sql = @"UPDATE dbo.Reservations 
                           SET Status = @Status 
                           WHERE Id = @Id";

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;

                    var parameters = new[]
                    {
                        CreateParameter(command, "@Status", status),
                        CreateParameter(command, "@Id", id)
                    };

                    command.Parameters.AddRange(parameters);

                    if (command.Connection.State != ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync();
                    }

                    await command.ExecuteNonQueryAsync();
                }

                TempData["SuccessMessage"] = "Reservation status updated successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating reservation status: {ex.Message}");
                TempData["ErrorMessage"] = "Error updating reservation status.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole))
            {
                return RedirectToDashboard();
            }

            if (!IsAdmin)
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var sql = @"UPDATE dbo.Reservations 
                           SET Status = 'Cancelled' 
                           WHERE Id = @Id";

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    command.Parameters.Add(CreateParameter(command, "@Id", id));

                    if (command.Connection.State != ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync();
                    }

                    await command.ExecuteNonQueryAsync();
                }

                TempData["SuccessMessage"] = "Reservation cancelled successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelling reservation: {ex.Message}");
                TempData["ErrorMessage"] = "Error cancelling reservation.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<int> AssignTableNumber(Reservation newReservation)
        {
            try
            {
                var sql = @"
                    SELECT TableNumber 
                    FROM dbo.Reservations 
                    WHERE CAST(ReservationDate AS DATE) = @ReservationDate 
                    AND Status != 'Cancelled' 
                    AND ABS(DATEDIFF(MINUTE, ReservationTime, @ReservationTime)) < 120";

                var existingTables = new List<int>();

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;

                    var parameters = new[]
                    {
                        CreateParameter(command, "@ReservationDate", newReservation.ReservationDate.Date),
                        CreateParameter(command, "@ReservationTime", newReservation.ReservationTime)
                    };

                    command.Parameters.AddRange(parameters);

                    if (command.Connection.State != ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync();
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            existingTables.Add(reader.GetInt32(0));
                        }
                    }
                }

                int tableNumber = 1;
                while (existingTables.Contains(tableNumber))
                {
                    tableNumber++;
                }

                return tableNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error assigning table number: {ex.Message}");
                throw;
            }
        }

        private static IDbDataParameter CreateParameter(IDbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            return parameter;
        }
    }
}