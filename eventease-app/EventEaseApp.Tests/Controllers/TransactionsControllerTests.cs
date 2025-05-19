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
    [DoNotParallelize]
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

            // Mock IWebHostEnvironment so WebRootPath points to a temp folder
            _envMock = new Mock<IWebHostEnvironment>();
            var tempRoot = Path.Combine(Path.GetTempPath(), "txn-tests");
            _envMock.Setup(env => env.WebRootPath).Returns(tempRoot);

            // Create directories for QR codes
            Directory.CreateDirectory(tempRoot);
            Directory.CreateDirectory(Path.Combine(tempRoot, "qr-codes"));

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
                try { Directory.Delete(qrFolder, true); }
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
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var view = (ViewResult)result;
            Assert.IsInstanceOfType(view.Model, typeof(TransactionCreateViewModel));

            var vm = (TransactionCreateViewModel)view.Model;
            Assert.AreEqual(42, vm.EventId);
            Assert.AreEqual(123.45m, vm.Amount);
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
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        /// <summary>
        /// POST Create with invalid ModelState should return the same ViewResult
        /// containing the original view-model.
        /// </summary>
        [TestMethod]
        public async Task Create_Post_InvalidModel_ReturnsViewWithSameModel()
        {
            // Arrange: dummy authenticated user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "1") }, "TestAuth"));
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
            _controller.ModelState.AddModelError("PaymentMethod", "Required");

            var badVm = new TransactionCreateViewModel { EventId = 42, Amount = 123.45m };

            // Act
            IActionResult result = await _controller.Create(badVm);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var view = (ViewResult)result;
            Assert.AreSame(badVm, view.Model);
        }

        /// <summary>
        /// POST Create with missing payment details should simulate a decline and
        /// return ViewResult with a ModelState error.
        /// </summary>
        [TestMethod]
        public async Task Create_Post_PaymentDeclined_ReturnsViewWithError()
        {
            // Arrange: dummy authenticated user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "3") }, "TestAuth"));
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
            _controller.ModelState.AddModelError("PaymentMethod", "PaymentMethod is required");

            var vm = new TransactionCreateViewModel { EventId = 42, Amount = 123.45m };

            // Act
            IActionResult result = await _controller.Create(vm);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsTrue(_controller.ModelState.ErrorCount > 0);
        }

        /// <summary>
        /// POST Create with valid payment details should save records, write QR-code, and redirect.
        /// </summary>
        [TestMethod]
        public async Task Create_Post_ValidPayment_SavesAndRedirects()
        {
            // Arrange
            var vm = new TransactionCreateViewModel { EventId = 42, Amount = 123.45m, PaymentMethod = "CreditCard", CardNumber = "4111111111111111" };
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "7") }, "TestAuth"));
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
            _controller.TempData = new TempDataDictionary(_controller.HttpContext, Mock.Of<ITempDataProvider>());

            // Act
            var result = await _controller.Create(vm);

            // Assert redirect
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirect = (RedirectToActionResult)result;
            Assert.AreEqual("MyTickets", redirect.ActionName);
            Assert.AreEqual("Tickets", redirect.ControllerName);

            // Assert DB writes
            var txn = await _context.Transactions.FirstAsync(t => t.UserId == 7 && t.EventId == 42);
            var ticket = await _context.Tickets.FirstAsync(t => t.UserId == 7 && t.EventId == 42);
            Assert.IsNotNull(txn);
            Assert.IsNotNull(ticket);

            // Assert QR code file
            var file = Path.Combine(_envMock.Object.WebRootPath, "qr-codes", ticket.QrCode + ".png");
            Assert.IsTrue(File.Exists(file));
        }
    }
}
