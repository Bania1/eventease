using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using eventease_app.Controllers;    // TransactionsController
using eventease_app.Models;         // EventEaseContext, TransactionCreateViewModel, Event, Transaction, Ticket

namespace EventEaseApp.Tests.Controllers
{
    /// <summary>
    /// Unit tests for TransactionsController.
    /// Covers GET Create and POST Create actions under various scenarios.
    /// </summary>
    [TestClass]
    public class TransactionsControllerTests
    {
        // Fields for in-memory context, environment mock, and controller under test
        private EventEaseContext _context = null!;
        private Mock<IWebHostEnvironment> _envMock = null!;
        private TransactionsController _controller = null!;

        /// <summary>
        /// Runs before each test: sets up an in-memory EF Core database, seeds an Event,
        /// mocks IWebHostEnvironment for file writes, and instantiates the controller.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            // Configure in-memory EF Core
            var opts = new DbContextOptionsBuilder<EventEaseContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new EventEaseContext(opts);

            // Seed one Event with Id=42 and Price=123.45
            _context.Events.Add(new Event { Id = 42, Price = 123.45m });
            _context.SaveChanges();

            // Mock IWebHostEnvironment to point WebRootPath to a temp folder
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath)
                    .Returns(Path.Combine(Path.GetTempPath(), "txn-tests"));

            // Instantiate the controller with context and mock environment
            _controller = new TransactionsController(_context, _envMock.Object);
        }

        /// <summary>
        /// Runs after each test: cleans up the in-memory database and deletes any created QR-code files.
        /// </summary>
        [TestCleanup]
        public void Teardown()
        {
            // Delete and dispose the in-memory database
            _context.Database.EnsureDeleted();
            _context.Dispose();

            // Safely delete the "qr-codes" folder if it exists
            var qrFolder = Path.Combine(_envMock.Object.WebRootPath, "qr-codes");
            if (Directory.Exists(qrFolder))
            {
                try
                {
                    Directory.Delete(qrFolder, recursive: true);
                }
                catch (IOException)
                {
                    // Ignore if files are locked
                }
            }
        }

        /// <summary>
        /// Test: GET Create with existing event Id should return a ViewResult
        /// and a TransactionCreateViewModel populated with EventId and Amount.
        /// </summary>
        [TestMethod]
        public void Create_Get_EventExists_ReturnsViewWithCorrectViewModel()
        {
            // Act: call GET Create with seeded event Id
            IActionResult result = _controller.Create(42);

            // Assert: we got a ViewResult
            Assert.IsInstanceOfType(result, typeof(ViewResult), "Expected a ViewResult");
            var view = (ViewResult)result;

            // Assert: model is TransactionCreateViewModel with correct properties
            Assert.IsInstanceOfType(view.Model, typeof(TransactionCreateViewModel), "Wrong VM type");
            var vm = (TransactionCreateViewModel)view.Model;
            Assert.AreEqual(42, vm.EventId, "EventId should match seeded event");
            Assert.AreEqual(123.45m, vm.Amount, "Amount should match event price");
        }

        /// <summary>
        /// Test: GET Create with non-existing event Id should return NotFoundResult.
        /// </summary>
        [TestMethod]
        public void Create_Get_EventDoesNotExist_ReturnsNotFound()
        {
            // Act: call GET Create with an invalid Id
            IActionResult result = _controller.Create(999);

            // Assert: we get a 404 NotFound
            Assert.IsInstanceOfType(result, typeof(NotFoundResult), "Unknown event should return 404");
        }

        /// <summary>
        /// Test: POST Create with invalid ModelState should return the same View
        /// with the original view-model, without touching the database.
        /// </summary>
        [TestMethod]
        public async Task Create_Post_InvalidModel_ReturnsViewWithSameModel()
        {
            // Arrange: set up a dummy authenticated user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            }, "TestAuth"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Force ModelState invalid (e.g. missing payment details)
            _controller.ModelState.AddModelError("PaymentMethod", "Required");

            var badVm = new TransactionCreateViewModel
            {
                EventId = 42,
                Amount = 123.45m
            };

            // Act: call POST Create
            IActionResult result = await _controller.Create(badVm);

            // Assert: same view returned with original VM
            Assert.IsInstanceOfType(result, typeof(ViewResult), "Expected a ViewResult on invalid model");
            var view = (ViewResult)result;
            Assert.AreSame(badVm, view.Model, "When ModelState is invalid, the same VM is returned");
        }

        /// <summary>
        /// Test: POST Create with missing payment details (payment declined)
        /// should return the view with a ModelState error.
        /// </summary>
        [TestMethod]
        public async Task Create_Post_PaymentDeclined_ReturnsViewWithError()
        {
            // Arrange: set up dummy user
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "3")
                    }, "TestAuth"))
                }
            };

            // Force ModelState invalid for decline scenario
            _controller.ModelState.AddModelError("PaymentMethod", "PaymentMethod is required");

            var vm = new TransactionCreateViewModel
            {
                EventId = 42,
                Amount = 123.45m
            };

            // Act: call POST Create
            IActionResult result = await _controller.Create(vm);

            // Assert: back to view with error
            Assert.IsInstanceOfType(result, typeof(ViewResult), "Expected a ViewResult when payment is declined");
            var view = (ViewResult)result;
            Assert.IsTrue(_controller.ModelState.ErrorCount > 0, "Expected a ModelState error on decline");
        }

        /// <summary>
        /// Test: POST Create with valid payment details should save a Transaction and Ticket,
        /// write a QR-code file to disk, and redirect to Tickets/MyTickets.
        /// </summary>
        [TestMethod]
        public async Task Create_Post_ValidPayment_SavesAndRedirects()
        {
            // Arrange: valid view-model with payment info
            var vm = new TransactionCreateViewModel
            {
                EventId = 42,
                Amount = 123.45m,
                PaymentMethod = "CreditCard",
                CardNumber = "4111111111111111"
            };

            // Simulate authenticated user ID = 7
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "7")
            }, "TestAuth"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act: call POST Create
            IActionResult result = await _controller.Create(vm);

            // Assert: redirected to Tickets/MyTickets
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult), "Expected redirect");
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("MyTickets", redirect.ActionName, "Redirect to MyTickets");
            Assert.AreEqual("Tickets", redirect.ControllerName, "Redirect to Tickets");

            // Assert: Transaction and Ticket saved in the database
            var txn = await _context.Transactions.FirstOrDefaultAsync(t => t.UserId == 7 && t.EventId == 42);
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.UserId == 7 && t.EventId == 42);
            Assert.IsNotNull(txn, "Transaction was not saved");
            Assert.IsNotNull(ticket, "Ticket was not saved");

            // Assert: QR-code PNG file was written
            var qrFolder = Path.Combine(_envMock.Object.WebRootPath, "qr-codes");
            var expectedFile = Path.Combine(qrFolder, ticket!.QrCode + ".png");
            Assert.IsTrue(File.Exists(expectedFile), "QR-code PNG was not written to disk");
        }
    }
}
