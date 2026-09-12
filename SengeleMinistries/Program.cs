using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using SengeleMinistries.Models;
using SengeleMinistries.Services;

var builder = WebApplication.CreateBuilder(args);


// =========================================================
// MVC + LOCALIZATION
// =========================================================
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();


// =========================================================
// SHOPPING CART SESSION
// =========================================================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
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
// LANGUAGE / LOCALIZATION
// English = default
// French = second language
// =========================================================
var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("fr")
};

var localizationOptions =
    new RequestLocalizationOptions
    {
        DefaultRequestCulture =
            new RequestCulture("en"),

        SupportedCultures =
            supportedCultures,

        SupportedUICultures =
            supportedCultures
    };

localizationOptions.RequestCultureProviders =
    new[]
    {
        new CookieRequestCultureProvider()
    };

app.UseRequestLocalization(localizationOptions);


// =========================================================
// SHOPPING CART SESSION
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