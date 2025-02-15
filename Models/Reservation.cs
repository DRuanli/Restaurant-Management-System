using System.ComponentModel.DataAnnotations;

namespace RestaurantSystem.Models
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Customer Name is required")]
        [StringLength(100, ErrorMessage = "Customer Name cannot exceed 100 characters")]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [Phone(ErrorMessage = "Invalid Phone Number format")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Number of Guests is required")]
        [Range(1, 20, ErrorMessage = "Number of Guests must be between 1 and 20")]
        [Display(Name = "Number of Guests")]
        public int NumberOfGuests { get; set; }

        [Required(ErrorMessage = "Reservation Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Reservation Date")]
        public DateTime ReservationDate { get; set; }

        [Required(ErrorMessage = "Reservation Time is required")]
        [Display(Name = "Reservation Time")]
        public TimeSpan ReservationTime { get; set; }

        public int TableNumber { get; set; }

        public string Status { get; set; } = "Pending";

        [StringLength(500, ErrorMessage = "Special Requests cannot exceed 500 characters")]
        [Display(Name = "Special Requests")]
        public string? SpecialRequests { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        // Read-only property for displaying the full reservation time
        public string FormattedTime => ReservationTime.ToString(@"hh\:mm");
    }
    // Custom validation attribute for future dates
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value is DateTime date)
            {
                return date.Date >= DateTime.Today;
            }
            return false;
        }
    }

    // Custom validation attribute for business hours
    public class BusinessHoursAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value is TimeSpan time)
            {
                var openTime = new TimeSpan(6, 0, 0); // 6 AM
                var closeTime = new TimeSpan(22, 0, 0); // 10 PM
                return time >= openTime && time <= closeTime;
            }
            return false;
        }
    }
}