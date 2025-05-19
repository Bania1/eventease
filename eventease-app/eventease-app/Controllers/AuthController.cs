using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using eventease_app.Models;
using eventease_app.Services;

namespace eventease_app.Controllers
{
    public class AuthController : Controller
    {
        private readonly EventEaseContext _context;
        private readonly IPasswordHasherService _hasher;

        public AuthController(EventEaseContext context, IPasswordHasherService hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public IActionResult Login()
        {
            return View();
        }

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

        public IActionResult Register()
        {
            return View();
        }

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

                model.PasswordHash = _hasher.HashPassword(model.PasswordHash);
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;
                model.Approved = false;

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

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // 1. GET anónimo: mostrar formulario “Forgot Password”
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // 2. POST anónimo: procesar el reseteo
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("", "Email and new password are required.");
                return View();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError("", "No account with that email.");
                return View();
            }

            // Simple reset: actualiza el hash
            user.PasswordHash = _hasher.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your password has been reset. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        public IActionResult ResetMyPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ResetMyPassword(string oldPassword, string newPassword)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login");

            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            if (user.PasswordHash != _hasher.HashPassword(oldPassword))
            {
                ModelState.AddModelError("", "Old password is incorrect.");
                return View();
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                ModelState.AddModelError("", "New password cannot be empty.");
                return View();
            }

            user.PasswordHash = _hasher.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password changed successfully!";
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(Login));
        }

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            var hashedInput = _hasher.HashPassword(inputPassword);
            return hashedInput == storedHash;
        }
    }
}
