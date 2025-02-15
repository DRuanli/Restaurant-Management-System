// Models/Feedback.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantSystem.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        public int? UserId { get; set; }

        [Required]
        [Range(1, 5)]
        public int FoodQualityRating { get; set; }

        [Required]
        [Range(1, 5)]
        public int ServiceRating { get; set; }

        [Required]
        [Range(1, 5)]
        public int AmbianceRating { get; set; }

        [Required]
        [Range(1, 5)]
        public int CleanlinessRating { get; set; }

        [Required]
        [Range(1, 5)]
        public int ValueForMoneyRating { get; set; }

        [StringLength(500)]
        public string Comments { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public virtual ICollection<DishFeedback> DishFeedbacks { get; set; }
    }

    public class DishFeedback
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FeedbackId { get; set; }

        [Required]
        public int MenuItemId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [ForeignKey("FeedbackId")]
        public virtual Feedback Feedback { get; set; }

        [ForeignKey("MenuItemId")]
        public virtual MenuItem MenuItem { get; set; }
    }
}