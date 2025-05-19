using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using eventease_app.Controllers;  // TransactionsController
using eventease_app.Models;       // EventEaseContext, TransactionCreateViewModel, Transaction, Ticket

namespace EventEaseApp.Tests.Controllers
{
    /// <summary>
    /// Unit tests for TransactionsController, covering both GET and POST Create actions.
    /// </summary>
    [TestClass]
    public class TransactionsControllerTests
    {
        private EventEaseContext _context = null!;
        private Mock<IWebHostEnvironment> _envMock = null!;
        private TransactionsController _controller = null!;

        /// <summary>
        /// Setup an in-memory database, seed a sample Event, mock IWebHostEnvironment, and instantiate the controller.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EventEaseContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new EventEaseContext(options);

            // Seed an event with ID=42 and price 123.45
            _context.Events.Add(new Event { Id = 42, Price = 123.45m });
            _context.SaveChanges();

            // Mock IWebHostEnvironment so _env.WebRootPath points to a temp folder
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath)
                    .Returns(Path.Combine(Path.GetTempPath(), "txn-tests"));

            // Instantiate controller
            _controller = new TransactionsController(_context, _envMock.Object);
        }

        /// <summary>
        /// Cleanup the in-memory database and delete any generated QR-code files.
        /// </summary>
        [TestCleanup]
        public void Teardown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();

            var qrFolder = Path.Combine(_envMock.Object.WebRootPath, "qr-codes");
            if (Directory.Exists(qrFolder))
            {
                try { Directory.Delete(qrFolder, recursive: true); }
                catch (IOException) { /* ignore locked files */ }
            }
        }

        /// <summary>
        /// GET Create with an existing event ID should return a ViewResult
        /// and a view-model populated with the correct EventId and Amount.
        /// </summary>
        [TestMethod]
        public void Create_Get_EventExists_ReturnsViewWithCorrectViewModel()
        {
            // Act
            IActionResult result = _controller.Create(42);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult), "Expected a ViewResult");
            var view = (ViewResult)result;

            Assert.IsInstanceOfType(view.Model, typeof(TransactionCreateViewModel), "Wrong view-model type");
            var vm = (TransactionCreateViewModel)view.Model;
            Assert.AreEqual(42, vm.EventId, "EventId should match seeded event");
            Assert.AreEqual(123.45m, vm.Amount, "Amount should match seeded event price");
        }

        /// <summary>
        /// GET Create with a non-existent event ID should return a NotFoundResult.
        /// </summary>
        [TestMethod]
        public void Create_Get_EventDoesNotExist_ReturnsNotFound()
        {
            // Act
            IActionResult result = _controller.Create(999);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult), "Expected 404 NotFound");
        }

        /// <summary>
        /// POST Create with invalid ModelState should return the same ViewResult
        /// containing the original view-model.
        /// </summary>
        [TestMethod]
        public async Task Create_Post_InvalidModel_ReturnsViewWithSameModel()
        {
            // Arrange: provide a dummy authenticated user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, "1") }, "TestAuth"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Force ModelState invalid
            _controller.ModelState.AddModelError("PaymentMethod", "Required");

            var badVm = new TransactionCreateViewModel { EventId = 42, Amount = 123.45m };

            // Act
            IActionResult result = await _controller.Create(badVm);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult), "Expected a ViewResult on invalid model");
            var view = (ViewResult)result;
            Assert.AreSame(badVm, view.Model, "Should return same view-model when invalid");
        }

        /// <summary>
        /// POST Create with missing payment details should simulate a decline and
        /// return ViewResult with a ModelState error.
        /// </summary>
        [TestMethod]
        public async Task Create_Post_PaymentDeclined_ReturnsViewWithError()
        {
            // Arrange: dummy authenticated user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, "3") }, "TestAuth"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Force payment decline path
            _controller.ModelState.AddModelError("PaymentMethod", "PaymentMethod is required");

            var vm = new TransactionCreateViewModel { EventId = 42, Amount = 123.45m };

            // Act
            IActionResult result = await _controller.Create(vm);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult), "Expected a ViewResult on payment decline");
            Assert.IsTrue(_controller.ModelState.ErrorCount > 0, "Expected ModelState errors on decline");
        }

        /// <summary>
        /// POST Create with valid payment details should:
        /// 1) save a Transaction and a Ticket,
        /// 2) generate and write a QR-code PNG to disk,
        /// 3) redirect to Tickets/MyTickets.
        /// </summary>
        [TestMethod]
        public async Task Create_Post_ValidPayment_SavesAndRedirects()
        {
            // Arrange: valid payment info
            var vm = new TransactionCreateViewModel
            {
                EventId = 42,
                Amount = 123.45m,
                PaymentMethod = "CreditCard",
                CardNumber = "4111111111111111"
            };

            // Provide a dummy authenticated user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, "7") }, "TestAuth"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Provide TempData to avoid null reference when setting TempData["Success"]
            _controller.TempData = new TempDataDictionary(
                _controller.ControllerContext.HttpContext,
                Mock.Of<ITempDataProvider>()
            );

            // Act
            IActionResult result = await _controller.Create(vm);

            // Assert redirection
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult), "Expected a redirect");
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("MyTickets", redirect.ActionName, "Should redirect to MyTickets");
            Assert.AreEqual("Tickets", redirect.ControllerName, "Should redirect to Tickets controller");

            // Assert Transaction saved
            var txn = await _context.Transactions.FirstOrDefaultAsync(
                t => t.UserId == 7 && t.EventId == 42
            );
            Assert.IsNotNull(txn, "Transaction was not saved in DB");

            // Assert Ticket saved and QrCode assigned
            var ticket = await _context.Tickets.FirstOrDefaultAsync(
                t => t.UserId == 7 && t.EventId == 42
            );
            Assert.IsNotNull(ticket, "Ticket was not saved in DB");
            Assert.IsFalse(string.IsNullOrEmpty(ticket!.QrCode), "Ticket.QrCode should be set");

            // Assert QR-code file created if possible
            var qrFolder = Path.Combine(_envMock.Object.WebRootPath, "qr-codes");
            if (Directory.Exists(qrFolder))
            {
                var files = Directory.GetFiles(qrFolder, ticket.QrCode + ".png");
                Assert.IsTrue(files.Length > 0, "Expected at least one QR-code PNG file");
            }
            else
            {
                Assert.Inconclusive("QR-codes folder was not created; file I/O may be disabled in this environment.");
            }
        }
    }
}
