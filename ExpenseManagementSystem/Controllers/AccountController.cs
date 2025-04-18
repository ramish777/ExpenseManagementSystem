using ExpenseManagementSystem.Models;
using ExpenseManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ExpenseManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;


        public AccountController(SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this._roleManager = roleManager;
            this._configuration = configuration;
        }

        // Private method 
        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            try
            {
                // Get roles for the user
                var roles = await userManager.GetRolesAsync(user);
                string rolesString = string.Join(", ", roles);

                // Create the claims, including roles
                var claims = new[]
                {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // Add roles here
                new Claim(ClaimTypes.Role, rolesString)
                };

                // Extract JWT configuration values
                var jwtSecret = _configuration["JWT:Secret"];
                var jwtIssuer = _configuration["JWT:Issuer"];
                var jwtAudience = _configuration["JWT:Audience"];
                var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret));
                var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddDays(30), // Set expiration to 30 days
                    SigningCredentials = signingCredentials,
                    Issuer = jwtIssuer,
                    Audience = jwtAudience
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);

                Log.Information("JWT token generated successfully for user {UserName}", user.UserName);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while generating JWT token for user: {UserName}", user.UserName);
                throw new ApplicationException("An error occurred while generating JWT token.");
            }
        }
        private IActionResult RedirectToHomePage()
        {
            try
            {
                var user = userManager.GetUserAsync(User).Result; // Use async/await in production
                var roles = userManager.GetRolesAsync(user).Result; // Use async/await in production

                TempData["Name"] = user.Name;
                TempData["Email"] = user.Email;
                TempData["UserId"] = user.Id;

                Log.Information("User {UserName} signed in. Redirecting based on role.", user.UserName);

                if (roles.Contains("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (roles.Contains("Manager"))
                {
                    return RedirectToAction("Index", "Manager");
                }
                else if (roles.Contains("Employee"))
                {
                    return RedirectToAction("Index", "Employee");
                }
                else
                {
                    return RedirectToAction("Index", "Accountant");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while redirecting to the homepage.");
                return RedirectToAction("Error", "Home"); // Redirect to an error page if necessary
            }
        }


        // GET: Login Page
        [HttpGet]
        public IActionResult Login()
        {
            try
            {
                if (signInManager.IsSignedIn(User))
                {
                    Log.Information("User is already signed in. Redirecting to the appropriate home page.");
                    // Redirect to the appropriate index page based on the user's role
                    return RedirectToHomePage();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while trying to load the login page.");
                return RedirectToAction("Error", "Home"); // Redirect to an error page if necessary
            }

            return View();
        }
        // POST: Process Login Form Submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            try
            {
                // Attempt to log the user in
                var result = await signInManager.PasswordSignInAsync(model.Username, model.Password, false, false);

                if (result.Succeeded)
                {
                    var user = await userManager.FindByNameAsync(model.Username);
                    if (user != null)
                    {
                        TempData["Name"] = user.Name;
                        TempData["Email"] = user.Email;
                        TempData["UserId"] = user.Id;

                        var roles = await userManager.GetRolesAsync(user);

                        Log.Information("User {UserName} logged in successfully.", user.UserName);

                        // Generate a JWT token with the user's roles
                        var token = await GenerateJwtToken(user);

                        if (roles.Contains("Admin"))
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                        else if (roles.Contains("Manager"))
                        {
                            return RedirectToAction("Index", "Manager");
                        }
                        else if (roles.Contains("Employee"))
                        {
                            return RedirectToAction("Index", "Employee");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Accountant");
                        }
                    }

                    ModelState.AddModelError("", "User not found");
                }
                else
                {
                    // Login attempt failed (incorrect password)
                    ViewBag.ErrorMessage = "Invalid username or password.";
                    Log.Warning("Failed login attempt for username: {Username}", model.Username);
                }
            }
            catch (Exception ex)
            {
               Log.Error(ex, "An error occurred while processing the login form for username: {Username}", model.Username);
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
            }

            // Return the login view with the errors
            return View(model);
        }
        // Logout action
        public async Task<IActionResult> Logout()
        {
            try
            {
                await signInManager.SignOutAsync();
                Log.Information("User signed out successfully.");
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during logout.");
                return RedirectToAction("Error", "Home");
            }
        }
        // Index action as an example, can be used for a dashboard or other content
        public IActionResult Index()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while loading the index page.");
                return RedirectToAction("Error", "Home");
            }
        }

       

    }
}
