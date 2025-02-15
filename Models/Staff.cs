using System.ComponentModel.DataAnnotations;

namespace RestaurantSystem.Models
{
    public class Staff
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string Position { get; set; }

        [Required]
        public string Department { get; set; }

        public string Status { get; set; } = "Active";

        public string? ImageUrl { get; set; }
    }

    public class StaffSchedule
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public DayOfWeek Day { get; set; }
        public string Shift { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class LeaveRequest
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
    }
}
