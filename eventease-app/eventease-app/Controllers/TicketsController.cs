using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using eventease_app.Models;
using System.Security.Claims;
using System.IO;
using QRCoder;
using Microsoft.AspNetCore.Hosting;

public class TicketsController : Controller
{
    private readonly EventEaseContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public TicketsController(EventEaseContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    // Method to buy a ticket
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Buy(int eventId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        // Check if the user has already purchased a ticket
        var existingTicket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.UserId == userId && t.EventId == eventId);

        if (existingTicket != null)
        {
            TempData["Error"] = "You have already purchased a ticket for this event.";
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        // Create a new ticket for the user
        var ticket = new Ticket
        {
            UserId = userId,
            EventId = eventId,
            QrCode = Guid.NewGuid().ToString(), // Generate a unique QR code
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Generate QR code image and save it
        var qrImagePath = GenerateQRCode(ticket.QrCode); // This method generates and saves the image

        // Optionally, you can store the QR code path in your database or display it

        TempData["Success"] = "Ticket purchased successfully!";
        return RedirectToAction("MyTickets", "Tickets");
    }

    // Method to view the user's tickets
    [Authorize]
    public async Task<IActionResult> MyTickets()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var tickets = await _context.Tickets
            .Where(t => t.UserId == userId)
            .Include(t => t.Event)  // Optionally include event details
            .ToListAsync();

        return View(tickets);  // Pass tickets to the view
    }

    // Method to generate the QR code image and save it
    private string GenerateQRCode(string ticketCode)
    {
        // Initialize the QRCodeGenerator
        using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
        {
            // Create the QR code data
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(ticketCode, QRCodeGenerator.ECCLevel.Q);

            // Generate the QRCode image
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                // Convert to byte array
                byte[] qrCodeImage = qrCode.GetGraphic(20);

                // Path to save the QR code image
                var folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "qr-codes");

                // Create directory if it doesn't exist
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // File path to save the image
                var filePath = Path.Combine(folderPath, $"{ticketCode}.png");

                // Save the QR code image as PNG
                System.IO.File.WriteAllBytes(filePath, qrCodeImage);

                return filePath;
            }
        }
    }
}
