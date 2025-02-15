using RestaurantSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure Entity Framework Core with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5242880; // 5MB in bytes
});
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// User-specific routes first (more specific)
app.MapControllerRoute(
    name: "user-reservation-create",
    pattern: "User/Create",
    defaults: new { controller = "User", action = "Create" });

app.MapControllerRoute(
    name: "user-reservations",
    pattern: "User/Reservations",
    defaults: new { controller = "User", action = "Reservations" });

app.MapControllerRoute(
    name: "user-orders",
    pattern: "User/Orders",
    defaults: new { controller = "User", action = "Orders" });

// Feature-specific routes
app.MapControllerRoute(
    name: "reservations",
    pattern: "Reservations/{action=Index}/{id?}",
    defaults: new { controller = "Reservations" });

app.MapControllerRoute(
    name: "orders",
    pattern: "Orders/{action=Index}/{id?}",
    defaults: new { controller = "Orders" });

// Controller-based routes
app.MapControllerRoute(
    name: "user",
    pattern: "User/{action=UserDashboard}/{id?}",
    defaults: new { controller = "User" });

app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=AdminDashboard}/{id?}",
    defaults: new { controller = "Admin" });

// Default route (most general - should be last)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

app.MapControllerRoute(
    name: "staff",
    pattern: "Staff/{action=Index}/{id?}",
    defaults: new { controller = "Staff" });

app.MapControllerRoute(
    name: "staff",
    pattern: "Staff/{action=Index}/{id?}",
    defaults: new { controller = "Staff" });