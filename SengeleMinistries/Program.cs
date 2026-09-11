using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Models;
using SengeleMinistries.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// MVC
// =========================================================
builder.Services.AddControllersWithViews();


// =========================================================
// SHOPPING CART SESSION
// =========================================================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    // Keep the shopping cart for 30 minutes of inactivity
    options.IdleTimeout = TimeSpan.FromMinutes(30);

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    options.Cookie.Name = ".SengeleMinistries.Cart";
});


// =========================================================
// DATABASE - SQL SERVER LOCALDB
// =========================================================
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection") ??
    "Server=(localdb)\\MSSQLLocalDB;Database=SengeleMinistries;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<SengeleMinistries.Data.ApplicationDbContext>(
    options =>
        options.UseSqlServer(connectionString)
);


// =========================================================
// EMAIL
// =========================================================
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddTransient<IEmailSender, MailKitEmailSender>();


// =========================================================
// AUTHENTICATION
// =========================================================
builder.Services
    .AddAuthentication(
        Microsoft.AspNetCore.Authentication.Cookies
            .CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Member/Login";
        options.LogoutPath = "/Member/Logout";

        options.Cookie.HttpOnly = true;

        options.Cookie.SameSite =
            Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    });


// =========================================================
// AUTHORIZATION
// =========================================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "AdminOnly",
        policy =>
            policy.RequireClaim("IsAdmin", "true"));
});


var app = builder.Build();


// =========================================================
// HTTP PIPELINE
// =========================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();


// =========================================================
// SHOPPING CART SESSION
// Must be before controllers/routes
// =========================================================
app.UseSession();


// =========================================================
// AUTHENTICATION / AUTHORIZATION
// =========================================================
app.UseAuthentication();

app.UseAuthorization();


// =========================================================
// STATIC FILES
// =========================================================
app.MapStaticAssets();


// =========================================================
// ADMIN AREA ROUTE
// =========================================================
app.MapControllerRoute(
    name: "areas",
    pattern:
        "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");


// =========================================================
// DEFAULT WEBSITE ROUTE
// =========================================================
app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();