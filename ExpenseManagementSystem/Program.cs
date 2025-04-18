using ExpenseManagementSystem.Data;
using ExpenseManagementSystem.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//get connection with db
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString)
);

// Configure Serilog to read settings from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.MSSqlServer(
       connectionString: connectionString,
       sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
       {
           TableName = "VpLogs",
           AutoCreateSqlTable = true
       }
    )
    .CreateLogger();

// Use Serilog as the logging provider
builder.Host.UseSerilog();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Account}/{action=Login}/{id?}");

// Seed roles and users
await SeedDatabase(app);

app.Run();

async Task SeedDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Seed roles
    string[] roleNames = { "Employee", "Admin", "Manager", "Accountant" };
    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Define users and their passwords
    // Define users and their passwords
    var users = new[]
    {
        // Admin
        new { UserName = "admin1@example.com", Password = "Admin@123", Role = "Admin", Name = "Admin User", ManagerUsername = (string?)null },
    
        // Managers
        new { UserName = "manager1@example.com", Password = "Manager@123", Role = "Manager",  Name = "Manager One", ManagerUsername = (string?)null },
        new { UserName = "manager2@example.com", Password = "Manager@456", Role = "Manager", Name = "Manager Two",  ManagerUsername = (string?)null },
    
        // Employees
        new { UserName = "employee1@example.com", Password = "Employee@123", Role = "Employee", Name = "Employee One",  ManagerUsername = "manager1@example.com" },
        new { UserName = "employee2@example.com", Password = "Employee@456", Role = "Employee", Name = "Employee Two",  ManagerUsername = "manager1@example.com" },
        new { UserName = "employee3@example.com", Password = "Employee@789", Role = "Employee", Name = "Employee Three",  ManagerUsername = "manager2@example.com" },
        new { UserName = "employee4@example.com", Password = "Employee@012", Role = "Employee", Name = "Employee Four",  ManagerUsername = "manager2@example.com" },
    
        // Accountant
        new { UserName = "accountant1@example.com", Password = "Accountant@123", Role = "Accountant", Name = "Accountant User",  ManagerUsername = (string?)null }
    };
    // Create users if they do not exist and assign roles
    foreach (var user in users)
    {
        var existingUser = await userManager.FindByNameAsync(user.UserName);
        if (existingUser == null)
        {
            var newUser = new ApplicationUser
            {
                UserName = user.UserName,
                Email = user.UserName,
                Name = user.Name,
                ManagerUsername = user.ManagerUsername
            };
            await userManager.CreateAsync(newUser, user.Password);
            await userManager.AddToRoleAsync(newUser, user.Role);
        }
        else
        {
            // Add user to role if not already in it
            if (!await userManager.IsInRoleAsync(existingUser, user.Role))
            {
                await userManager.AddToRoleAsync(existingUser, user.Role);
            }
        }
    }
}