using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using eventease_app.Models;
using System.Security.Claims;
using System.IO;
using QRCoder;
using Microsoft.AspNetCore.Hosting;

namespace eventease_app.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly EventEaseContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TicketsController(EventEaseContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // POST: /Tickets/Buy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(int eventId)
        {
            // Safely get the user ID claim
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Challenge(); // or RedirectToAction("Login", "Account");

            // Prevent double-purchase
            if (await _context.Tickets.AnyAsync(t => t.UserId == userId && t.EventId == eventId))
            {
                TempData["Error"] = "You’ve already purchased a ticket for this event.";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            // Create & save the ticket
            var ticket = new Ticket
            {
                UserId = userId,
                EventId = eventId,
                QrCode = Guid.NewGuid().ToString()
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Generate and save the QR-code image
            GenerateQRCodeImage(ticket.QrCode);

            TempData["Success"] = "Ticket purchased successfully!";
            return RedirectToAction(nameof(MyTickets));
        }

        // GET: /Tickets/MyTickets
        [HttpGet]
        public async Task<IActionResult> MyTickets()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Challenge();

            var tickets = await _context.Tickets
                .Where(t => t.UserId == userId)
                .Include(t => t.Event)
                .ToListAsync();

            return View(tickets);
        }

        // POST: /Tickets/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Challenge();

            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (ticket == null)
            {
                TempData["Error"] = "Ticket not found or you don’t have permission to delete it.";
                return RedirectToAction(nameof(MyTickets));
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ticket deleted successfully.";
            return RedirectToAction(nameof(MyTickets));
        }

        // Helper: generate & write the PNG to wwwroot/qr-codes
        private void GenerateQRCodeImage(string code)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(code, QRCodeGenerator.ECCLevel.Q);
            using var qrPng = new PngByteQRCode(qrData);
            var bytes = qrPng.GetGraphic(20);

            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "qr-codes");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, $"{code}.png");
            System.IO.File.WriteAllBytes(filePath, bytes);
        }
    }
}
