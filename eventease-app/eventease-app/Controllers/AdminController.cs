using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eventease_app.Models;

namespace eventease_app.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly EventEaseContext _context;

        public AdminController(EventEaseContext context)
        {
            _context = context;
        }

        // View Pending Organizers
        public async Task<IActionResult> PendingOrganizers()
        {
            var pendingOrganizers = await _context.Users
                .Where(u => u.Role == "organizer" && !u.Approved)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            return View(pendingOrganizers);
        }

        // Approve Organizer
        [HttpPost]
        public async Task<IActionResult> ApproveOrganizer(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Role == "organizer")
            {
                user.Approved = true;
                user.UpdatedAt = DateTime.UtcNow;
                _context.Update(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(PendingOrganizers));
        }

        // 🔥 View All Users
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.OrderBy(u => u.CreatedAt).ToListAsync();
            return View(users);
        }

        // 🔥 Edit User Role
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, User user)
        {
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser != null)
                    {
                        existingUser.Role = user.Role;
                        existingUser.Approved = user.Approved;
                        existingUser.UpdatedAt = DateTime.UtcNow;

                        _context.Update(existingUser);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
                return RedirectToAction(nameof(Users));
            }

            return View(user);
        }

        // 🔥 Delete User
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/ResetPassword/5
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Admin/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(newPassword))
            {
                user.PasswordHash = HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;
                _context.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Password reset for {user.Email}!";
                return RedirectToAction(nameof(Users));
            }

            TempData["ErrorMessage"] = "Password cannot be empty.";
            return View(user);
        }

        // Helper function
        private string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

    }
}
