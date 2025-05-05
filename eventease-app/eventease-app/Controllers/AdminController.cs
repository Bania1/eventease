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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOrganizer(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role.ToLower() != "organizer")
                return NotFound();

            user.Approved = true;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Organizer '{user.Email}' approved successfully!";

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
        public async Task<IActionResult> EditUser(int id, User formUser)
        {
            if (id != formUser.Id)
                return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(user); // return full model, not formUser

            // Update fields
            user.Role = formUser.Role;
            user.Approved = formUser.Approved;
            user.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(formUser.NewPassword))
            {
                user.PasswordHash = _hasher.HashPassword(formUser.NewPassword);
            }

            _context.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Users));
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
