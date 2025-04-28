using Microsoft.AspNetCore.Mvc;
using eventease_app.Models;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace eventease_app.Controllers
{
    public class AuthController : Controller
    {
        private readonly EventEaseContext _context; // Your EF database context

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
        public IActionResult Login(User model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

                if (user != null && VerifyPassword(model.PasswordHash, user.PasswordHash))
                {
                    // TODO: Set up authentication session/cookie here
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
        public IActionResult Register(User model)
        {
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);
                }

                model.PasswordHash = HashPassword(model.PasswordHash);
                model.Role = "user"; // Default role
                model.Approved = true; // You can set to false if you want admin approval
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;

                _context.Users.Add(model);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // Helper to hash a password
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        // Helper to verify password
        private bool VerifyPassword(string enteredPassword, string storedPasswordHash)
        {
            var hashOfInput = HashPassword(enteredPassword);
            return hashOfInput == storedPasswordHash;
        }
    }
}
