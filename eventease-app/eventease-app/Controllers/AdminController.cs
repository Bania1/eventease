using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eventease_app.Models;
using eventease_app.Services;

namespace eventease_app.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly EventEaseContext _context;
        private readonly IPasswordHasherService _hasher;

        public AdminController(EventEaseContext context, IPasswordHasherService hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public async Task<IActionResult> PendingOrganizers()
        {
            var pendingOrganizers = await _context.Users
                .Where(u => u.Role == "organizer" && !u.Approved)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            return View(pendingOrganizers);
        }

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

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.OrderBy(u => u.CreatedAt).ToListAsync();
            return View(users);
        }

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

                        if (!string.IsNullOrEmpty(user.PasswordHash))
                        {
                            existingUser.PasswordHash = _hasher.HashPassword(user.PasswordHash);
                        }

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

        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(newPassword))
            {
                user.PasswordHash = _hasher.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;
                _context.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Password reset for {user.Email}!";
                return RedirectToAction(nameof(Users));
            }

            TempData["ErrorMessage"] = "Password cannot be empty.";
            return View(user);
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.UserCount = await _context.Users.CountAsync();
            ViewBag.PendingOrganizersCount = await _context.Users.CountAsync(u => u.Role == "organizer" && !u.Approved);
            ViewBag.PublishedEventsCount = await _context.Events.CountAsync(e => e.IsPublished);
            ViewBag.TransactionsCount = await _context.Transactions.CountAsync();

            return View();
        }
    }
}
