using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Models;
using RestaurantSystem.Enums;

namespace RestaurantSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<DishFeedback> DishFeedbacks { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<StaffSchedule> StaffSchedules { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Existing User seed data
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", Password = "admin123", Role = "admin" },
                new User { Id = 2, Username = "user1", Password = "user123", Role = "user" },
                new User { Id = 3, Username = "user2", Password = "user456", Role = "user" }
            );

            modelBuilder.Entity<Staff>().HasData(
                new Staff
                {
                    Id = 1,
                    Name = "Admin Staff",
                    Position = "Manager",
                    Department = "Management",
                    Email = "admin@restaurant.com",
                    Phone = "123-456-7890",
                    Status = "Active",
                    ImageUrl = null
                }
            );
            // MenuItem seed data
            modelBuilder.Entity<MenuItem>().HasData(
                new MenuItem
                {
                    Id = 1,
                    Name = "Classic Burger",
                    Description = "Juicy beef patty with lettuce, tomato, cheese, and special sauce",
                    Price = 12.99M,
                    Category = "Main Course",
                    IsAvailable = true,
                    ImageUrl = null
                },
                new MenuItem
                {
                    Id = 2,
                    Name = "Grilled Salmon",
                    Description = "Fresh salmon fillet with lemon herb butter and seasonal vegetables",
                    Price = 24.99M,
                    Category = "Main Course",
                    IsAvailable = true,
                    ImageUrl = null
                },
                new MenuItem
                {
                    Id = 3,
                    Name = "Caesar Salad",
                    Description = "Crisp romaine lettuce, parmesan cheese, croutons with classic Caesar dressing",
                    Price = 10.99M,
                    Category = "Main Course",
                    IsAvailable = true,
                    ImageUrl = null
                },
                new MenuItem
                {
                    Id = 4,
                    Name = "Coca Cola",
                    Description = "Classic refreshing cola drink",
                    Price = 2.99M,
                    Category = "Beverages",
                    IsAvailable = true,
                    ImageUrl = null
                },
                new MenuItem
                {
                    Id = 5,
                    Name = "Fresh Orange Juice",
                    Description = "Freshly squeezed orange juice",
                    Price = 3.99M,
                    Category = "Beverages",
                    IsAvailable = true,
                    ImageUrl = null
                },
                new MenuItem
                {
                    Id = 6,
                    Name = "Margherita Pizza",
                    Description = "Fresh mozzarella, tomatoes, and basil on thin crust",
                    Price = 15.99M,
                    Category = "Main Course",
                    IsAvailable = true,
                    ImageUrl = null
                }
            );

            // Configure relationships
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.MenuItem)
                .WithMany()
                .HasForeignKey(oi => oi.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Order)
                .WithMany()
                .HasForeignKey(f => f.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DishFeedback>()
                .HasOne(df => df.Feedback)
                .WithMany(f => f.DishFeedbacks)
                .HasForeignKey(df => df.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DishFeedback>()
                .HasOne(df => df.MenuItem)
                .WithMany()
                .HasForeignKey(df => df.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Staff>()
                .Property(s => s.ImageUrl)
                .IsRequired(false);
        }

    }
}