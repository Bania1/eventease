using eventease_app.Models;  // Make sure this matches the namespace where EventEaseContext is defined
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1) Add services to the container
builder.Services.AddControllersWithViews();

// 2) Register the DbContext with the connection string from appsettings.json
//    Ensure the key in GetConnectionString("eventeaseDb") exactly matches appsettings.json.
builder.Services.AddDbContext<EventEaseContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("eventeaseDb")
    )
);

var app = builder.Build();

// 3) Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// 4) Map default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// 5) Run the app
app.Run();
