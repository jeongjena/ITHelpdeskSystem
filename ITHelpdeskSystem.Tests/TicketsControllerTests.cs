using ITHelpdeskSystem.Controllers;
using ITHelpdeskSystem.Data;
using ITHelpdeskSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ITHelpdeskSystem.Tests
{
    [TestClass]
    public class TicketsControllerTests
    {
        private SqliteConnection _connection = null!;
        private ApplicationDbContext _context = null!;
        private TicketsController _controller = null!;

        [TestInitialize]
        public void Setup()
        {
            // Creates a temporary SQLite database for each test.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(_connection)
                    .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _controller = new TicketsController(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [TestMethod]
        public async Task Create_ValidTicket_ShouldSaveToDatabase()
        {
            var ticket = CreateValidTicket();

            await _controller.Create(ticket);

            Assert.AreEqual(1, _context.Tickets.Count());

            var savedTicket = _context.Tickets.Single();

            Assert.AreEqual("Aarti", savedTicket.RequesterName);
            Assert.AreEqual("aarti@example.com", savedTicket.RequesterEmail);
            Assert.AreEqual("Laptop not turning on", savedTicket.Title);
        }

        [TestMethod]
        public async Task Create_InvalidTicket_ShouldNotSaveToDatabase()
        {
            var ticket = CreateValidTicket();

            _controller.ModelState.AddModelError(
                nameof(Ticket.Title),
                "Title is required.");

            var result = await _controller.Create(ticket);

            Assert.AreEqual(0, _context.Tickets.Count());
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task Create_ValidTicket_ShouldRecordCreatedAt()
        {
            var ticket = CreateValidTicket();

            var beforeCreation = DateTime.UtcNow;

            await _controller.Create(ticket);

            var afterCreation = DateTime.UtcNow;

            var savedTicket = _context.Tickets.Single();

            Assert.IsTrue(savedTicket.CreatedAt >= beforeCreation);
            Assert.IsTrue(savedTicket.CreatedAt <= afterCreation);
        }

        [TestMethod]
        public async Task Create_ValidTicket_ShouldUseDefaultWorkflowValues()
        {
            var ticket = CreateValidTicket();

            // Simulates values that should not be controlled by the employee.
            ticket.Priority = TicketPriority.High;
            ticket.Status = TicketStatus.Resolved;
            ticket.AssignedTechnician = "Alex";
            ticket.TriagedAt = DateTime.UtcNow;
            ticket.ResolvedAt = DateTime.UtcNow;

            await _controller.Create(ticket);

            var savedTicket = _context.Tickets.Single();

            Assert.AreEqual(
                TicketPriority.Unassigned,
                savedTicket.Priority);

            Assert.AreEqual(
                TicketStatus.Open,
                savedTicket.Status);

            Assert.IsNull(savedTicket.AssignedTechnician);
            Assert.IsNull(savedTicket.TriagedAt);
            Assert.IsNull(savedTicket.ResolvedAt);
        }

        [TestMethod]
        public async Task Create_ValidTicket_ShouldRedirectToConfirmation()
        {
            var ticket = CreateValidTicket();

            var result = await _controller.Create(ticket);

            Assert.IsInstanceOfType(
                result,
                typeof(RedirectToActionResult));

            var redirect = (RedirectToActionResult)result;

            Assert.AreEqual("Confirmation", redirect.ActionName);
            Assert.IsTrue(ticket.Id > 0);
        }

        [TestMethod]
        public async Task Confirmation_ExistingTicket_ShouldReturnView()
        {
            var ticket = await AddOpenTicket();

            var result = await _controller.Confirmation(ticket.Id);

            Assert.IsInstanceOfType(result, typeof(ViewResult));

            var view = (ViewResult)result;

            Assert.IsNotNull(view.Model);
        }

        [TestMethod]
        public async Task Confirmation_UnknownTicket_ShouldReturnNotFound()
        {
            var result = await _controller.Confirmation(999);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Index_ShouldReturnStoredTickets()
        {
            await AddOpenTicket();
            await AddOpenTicket();

            var result = await _controller.Index();

            Assert.IsInstanceOfType(result, typeof(ViewResult));

            var view = (ViewResult)result;
            var tickets = view.Model as List<Ticket>;

            Assert.IsNotNull(tickets);
            Assert.AreEqual(2, tickets.Count);
        }
        [TestMethod]
        public async Task Details_ExistingTicket_ShouldReturnView()
        {
            var ticket = await AddOpenTicket();

            var result = await _controller.Details(ticket.Id);

            Assert.IsInstanceOfType(result, typeof(ViewResult));

            var view = (ViewResult)result;
            var model = view.Model as Ticket;

            Assert.IsNotNull(model);
            Assert.AreEqual(ticket.Id, model.Id);
        }

        [TestMethod]
        public async Task Details_UnknownTicket_ShouldReturnNotFound()
        {
            var result = await _controller.Details(999);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Triage_GetOpenTicket_ShouldReturnView()
        {
            var ticket = await AddOpenTicket();

            var result = await _controller.Triage(ticket.Id);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task Triage_GetNonOpenTicket_ShouldRedirectToIndex()
        {
            var ticket = await AddOpenTicket();

            ticket.Status = TicketStatus.InProgress;
            await _context.SaveChangesAsync();

            var result = await _controller.Triage(ticket.Id);

            Assert.IsInstanceOfType(
                result,
                typeof(RedirectToActionResult));

            var redirect = (RedirectToActionResult)result;

            Assert.AreEqual("Index", redirect.ActionName);
        }

        [TestMethod]
        public async Task Triage_ValidInput_ShouldUpdateTicket()
        {
            var ticket = await AddOpenTicket();

            var result = await _controller.Triage(
                ticket.Id,
                TicketPriority.High,
                "Alex");

            var updatedTicket =
                await _context.Tickets.FindAsync(ticket.Id);

            Assert.IsNotNull(updatedTicket);

            Assert.AreEqual(
                TicketPriority.High,
                updatedTicket.Priority);

            Assert.AreEqual(
                "Alex",
                updatedTicket.AssignedTechnician);

            Assert.AreEqual(
                TicketStatus.InProgress,
                updatedTicket.Status);

            Assert.IsNotNull(updatedTicket.TriagedAt);

            Assert.IsInstanceOfType(
                result,
                typeof(RedirectToActionResult));
        }

        [TestMethod]
        public async Task Triage_ValidInput_ShouldRecordTriagedAt()
        {
            var ticket = await AddOpenTicket();

            var beforeTriage = DateTime.UtcNow;

            await _controller.Triage(
                ticket.Id,
                TicketPriority.Medium,
                "Alex");

            var afterTriage = DateTime.UtcNow;

            var updatedTicket =
                await _context.Tickets.FindAsync(ticket.Id);

            Assert.IsNotNull(updatedTicket);
            Assert.IsNotNull(updatedTicket.TriagedAt);

            Assert.IsTrue(
                updatedTicket.TriagedAt >= beforeTriage);

            Assert.IsTrue(
                updatedTicket.TriagedAt <= afterTriage);
        }

        [TestMethod]
        public async Task Triage_UnassignedPriority_ShouldNotCompleteTriage()
        {
            var ticket = await AddOpenTicket();

            var result = await _controller.Triage(
                ticket.Id,
                TicketPriority.Unassigned,
                "Alex");

            var updatedTicket =
                await _context.Tickets.FindAsync(ticket.Id);

            Assert.IsNotNull(updatedTicket);
            Assert.AreEqual(
                TicketStatus.Open,
                updatedTicket.Status);

            Assert.AreEqual(
                TicketPriority.Unassigned,
                updatedTicket.Priority);

            Assert.IsNull(updatedTicket.TriagedAt);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task Triage_MissingTechnician_ShouldNotCompleteTriage()
        {
            var ticket = await AddOpenTicket();

            var result = await _controller.Triage(
                ticket.Id,
                TicketPriority.High,
                "");

            var updatedTicket =
                await _context.Tickets.FindAsync(ticket.Id);

            Assert.IsNotNull(updatedTicket);
            Assert.AreEqual(
                TicketStatus.Open,
                updatedTicket.Status);

            Assert.IsNull(updatedTicket.AssignedTechnician);
            Assert.IsNull(updatedTicket.TriagedAt);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task Resolve_GetInProgressTicket_ShouldReturnView()
        {
            var ticket = await AddInProgressTicket();

            var result = await _controller.Resolve(ticket.Id);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task Resolve_GetOpenTicket_ShouldRedirectToIndex()
        {
            var ticket = await AddOpenTicket();

            var result = await _controller.Resolve(ticket.Id);

            Assert.IsInstanceOfType(
                result,
                typeof(RedirectToActionResult));

            var redirect = (RedirectToActionResult)result;

            Assert.AreEqual("Index", redirect.ActionName);
        }

        [TestMethod]
        public async Task Resolve_GetAlreadyResolvedTicket_ShouldRedirectToIndex()
        {
            var ticket = await AddResolvedTicket();

            var result = await _controller.Resolve(ticket.Id);

            Assert.IsInstanceOfType(
                result,
                typeof(RedirectToActionResult));

            var redirect = (RedirectToActionResult)result;

            Assert.AreEqual("Index", redirect.ActionName);
        }

        [TestMethod]
        public async Task Resolve_GetUnknownTicket_ShouldReturnNotFound()
        {
            var result = await _controller.Resolve(999);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task ResolveConfirmed_InProgressTicket_ShouldUpdateTicket()
        {
            var ticket = await AddInProgressTicket();

            var result = await _controller.ResolveConfirmed(ticket.Id);

            var updatedTicket =
                await _context.Tickets.FindAsync(ticket.Id);

            Assert.IsNotNull(updatedTicket);

            Assert.AreEqual(
                TicketStatus.Resolved,
                updatedTicket.Status);

            Assert.IsNotNull(updatedTicket.ResolvedAt);

            Assert.IsInstanceOfType(
                result,
                typeof(RedirectToActionResult));

            var redirect = (RedirectToActionResult)result;

            Assert.AreEqual("Index", redirect.ActionName);
        }

        [TestMethod]
        public async Task ResolveConfirmed_InProgressTicket_ShouldRecordResolvedAt()
        {
            var ticket = await AddInProgressTicket();

            var beforeResolve = DateTime.UtcNow;

            await _controller.ResolveConfirmed(ticket.Id);

            var afterResolve = DateTime.UtcNow;

            var updatedTicket =
                await _context.Tickets.FindAsync(ticket.Id);

            Assert.IsNotNull(updatedTicket);
            Assert.IsNotNull(updatedTicket.ResolvedAt);

            Assert.IsTrue(
                updatedTicket.ResolvedAt >= beforeResolve);

            Assert.IsTrue(
                updatedTicket.ResolvedAt <= afterResolve);
        }

        [TestMethod]
        public async Task ResolveConfirmed_OpenTicket_ShouldNotUpdateTicket()
        {
            var ticket = await AddOpenTicket();

            var result = await _controller.ResolveConfirmed(ticket.Id);

            var updatedTicket =
                await _context.Tickets.FindAsync(ticket.Id);

            Assert.IsNotNull(updatedTicket);

            // The ticket must remain Open; only InProgress tickets can be resolved.
            Assert.AreEqual(
                TicketStatus.Open,
                updatedTicket.Status);

            Assert.IsNull(updatedTicket.ResolvedAt);

            Assert.IsInstanceOfType(
                result,
                typeof(RedirectToActionResult));
        }

        [TestMethod]
        public async Task ResolveConfirmed_AlreadyResolvedTicket_ShouldNotChangeResolvedAt()
        {
            var ticket = await AddResolvedTicket();
            var originalResolvedAt = ticket.ResolvedAt;

            await _controller.ResolveConfirmed(ticket.Id);

            var updatedTicket =
                await _context.Tickets.FindAsync(ticket.Id);

            Assert.IsNotNull(updatedTicket);

            // Resolving an already-resolved ticket should not overwrite the original timestamp.
            Assert.AreEqual(
                originalResolvedAt,
                updatedTicket.ResolvedAt);
        }

        [TestMethod]
        public async Task ResolveConfirmed_UnknownTicket_ShouldReturnNotFound()
        {
            var result = await _controller.ResolveConfirmed(999);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }
        private static Ticket CreateValidTicket()
        {
            return new Ticket
            {
                RequesterName = "Aarti",
                RequesterEmail = "aarti@example.com",
                Title = "Laptop not turning on",
                Description = "Screen stays black on power up"
            };
        }

        private async Task<Ticket> AddOpenTicket()
        {
            var ticket = CreateValidTicket();

            ticket.Priority = TicketPriority.Unassigned;
            ticket.Status = TicketStatus.Open;
            ticket.AssignedTechnician = null;
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.TriagedAt = null;
            ticket.ResolvedAt = null;

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return ticket;
        }

        private async Task<Ticket> AddInProgressTicket()
        {
            var ticket = CreateValidTicket();

            ticket.Priority = TicketPriority.High;
            ticket.Status = TicketStatus.InProgress;
            ticket.AssignedTechnician = "Alex";
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.TriagedAt = DateTime.UtcNow;
            ticket.ResolvedAt = null;

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return ticket;
        }

        private async Task<Ticket> AddResolvedTicket()
        {
            var ticket = CreateValidTicket();

            ticket.Priority = TicketPriority.High;
            ticket.Status = TicketStatus.Resolved;
            ticket.AssignedTechnician = "Alex";
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.TriagedAt = DateTime.UtcNow;
            ticket.ResolvedAt = DateTime.UtcNow;

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return ticket;
        }
    }
}
