using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Models;
using SengeleMinistries.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Add EF Core DbContext (SQL Server LocalDB) for application data
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    "Server=(localdb)\\MSSQLLocalDB;Database=SengeleMinistries;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
builder.Services.AddDbContext<SengeleMinistries.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// Bind EmailSettings from configuration (appsettings.json) and register MailKit sender
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, MailKitEmailSender>();

// Configure cookie authentication (simple, non-Identity)
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Member/Login";
        options.LogoutPath = "/Member/Logout";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    });

// Authorization policy for admin-only access
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
