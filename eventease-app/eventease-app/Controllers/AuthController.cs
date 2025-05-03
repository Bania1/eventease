using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using eventease_app.Models;

namespace eventease_app.Controllers
{
    public class AuthController : Controller
    {
        private readonly EventEaseContext _context;

        public AuthController(EventEaseContext context)
        {
            _context = context;
        }

        // GET: Auth/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(User model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

                if (user != null && VerifyPassword(model.PasswordHash, user.PasswordHash))
                {
                    if (user.Role == "organizer" && !user.Approved)
                    {
                        ModelState.AddModelError("", "Your organizer account is awaiting admin approval.");
                        return View(model);
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim(ClaimTypes.Name, user.Email)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTime.UtcNow.AddHours(2)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties
                    );

                    TempData["SuccessMessage"] = "Login successful!";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                }
            }
            return View(model);
        }

        // GET: Auth/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model)
        {
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("", "Email already registered.");
                    return View(model);
                }

                model.PasswordHash = HashPassword(model.PasswordHash);
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;
                model.Approved = false; // Always unapproved initially

                if (string.IsNullOrEmpty(model.Role))
                {
                    model.Role = "user";
                }

                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registration successful. Please login!";
                return RedirectToAction(nameof(Login));
            }

            return View(model);
        }

        // POST: Auth/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // ✅ RESET MY PASSWORD (Self-service)
        
        // GET: Auth/ResetMyPassword
        [Authorize]
        public IActionResult ResetMyPassword()
        {
            return View();
        }

        // POST: Auth/ResetMyPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ResetMyPassword(string oldPassword, string newPassword)
        {
            //var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login");

            var userId = int.Parse(userIdClaim);

            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            if (user.PasswordHash != HashPassword(oldPassword))
            {
                ModelState.AddModelError("", "Old password is incorrect.");
                return View();
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                ModelState.AddModelError("", "New password cannot be empty.");
                return View();
            }

            user.PasswordHash = HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password changed successfully!";
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(Login));
        }

        // Helper methods

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            var hashedInput = HashPassword(inputPassword);
            return hashedInput == storedHash;
        }
    }
}
