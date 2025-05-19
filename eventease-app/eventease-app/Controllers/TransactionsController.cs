using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using QRCoder;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using eventease_app.Models;

namespace eventease_app.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly EventEaseContext _context;
        private readonly IWebHostEnvironment _env;

        public TransactionsController(EventEaseContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Transactions/Create?eventId=123
        [HttpGet]
        public IActionResult Create(int eventId)
        {
            var ev = _context.Events.Find(eventId);
            if (ev == null) return NotFound();

            var vm = new TransactionCreateViewModel
            {
                EventId = ev.Id,
                Amount = ev.Price
            };
            return View(vm);
        }

        // POST: Transactions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionCreateViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // --- Payment simulation; replace with your real gateway integration ---
            bool paymentOk = vm.PaymentMethod != ""
                             && (vm.PaymentMethod != "CreditCard"
                                 || !string.IsNullOrEmpty(vm.CardNumber));
            if (!paymentOk)
            {
                ModelState.AddModelError("", "Payment was declined. Please check your details.");
                return View(vm);
            }

            // 1) Save Transaction
            var txn = new Transaction
            {
                UserId = userId,
                EventId = vm.EventId,
                Amount = vm.Amount,
                PaymentMethod = vm.PaymentMethod,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Transactions.Add(txn);
            await _context.SaveChangesAsync();

            // 2) Save Ticket
            var ticket = new Ticket
            {
                UserId = userId,
                EventId = vm.EventId,
                QrCode = Guid.NewGuid().ToString()
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // 3) Generate and save QR image
            var folder = Path.Combine(_env.WebRootPath, "qr-codes");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            using var qrGen = new QRCodeGenerator();
            var qrData = qrGen.CreateQrCode(ticket.QrCode, QRCodeGenerator.ECCLevel.Q);
            using var qrPng = new PngByteQRCode(qrData);
            var qrBytes = qrPng.GetGraphic(20);
            var qrPath = Path.Combine(folder, $"{ticket.QrCode}.png");
            System.IO.File.WriteAllBytes(qrPath, qrBytes);

            TempData["Success"] = "Payment confirmed! Your ticket is ready.";
            return RedirectToAction("MyTickets", "Tickets");
        }
    }
}
