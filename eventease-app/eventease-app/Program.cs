using eventease_app.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using eventease_app.Services;

var builder = WebApplication.CreateBuilder(args);

// 1) Add services to the container
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<EventEaseContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("eventeaseDb")
    )
);

// ? Register authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
    });

// ? Register authorization
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();


// ? Register IHttpContextAccessor so Razor views like _LoginPartial.cshtml can access user info
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 2) Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ? Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// 3) Map default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// 4) Run the app
app.Run();
