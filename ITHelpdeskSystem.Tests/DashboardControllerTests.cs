using ITHelpdeskSystem.Controllers;
using ITHelpdeskSystem.Data;
using ITHelpdeskSystem.Models;
using ITHelpdeskSystem.Services;
using ITHelpdeskSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ITHelpdeskSystem.Tests
{
    [TestClass]
    public class DashboardControllerTests
    {
        private SqliteConnection _connection = null!;
        private ApplicationDbContext _context = null!;
        private DashboardController _controller = null!;

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

            _controller = new DashboardController(
                _context,
                new SlaService());
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [TestMethod]
        public async Task Index_ShouldReturnCorrectCountsByStatus()
        {
            await AddTicket(TicketStatus.Open, TicketPriority.Unassigned, DateTime.UtcNow, null);
            await AddTicket(TicketStatus.Open, TicketPriority.Unassigned, DateTime.UtcNow, null);
            await AddTicket(TicketStatus.InProgress, TicketPriority.Medium, DateTime.UtcNow, DateTime.UtcNow);
            await AddTicket(TicketStatus.Resolved, TicketPriority.Low, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow);

            var result = await _controller.Index();

            Assert.IsInstanceOfType(result, typeof(ViewResult));

            var view = (ViewResult)result;
            var model = view.Model as DashboardViewModel;

            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.OpenCount);
            Assert.AreEqual(1, model.InProgressCount);
            Assert.AreEqual(1, model.ResolvedCount);
        }

        [TestMethod]
        public async Task Index_ShouldReturnCorrectCountsByPriority()
        {
            await AddTicket(TicketStatus.InProgress, TicketPriority.High, DateTime.UtcNow, DateTime.UtcNow);
            await AddTicket(TicketStatus.InProgress, TicketPriority.High, DateTime.UtcNow, DateTime.UtcNow);
            await AddTicket(TicketStatus.InProgress, TicketPriority.Medium, DateTime.UtcNow, DateTime.UtcNow);
            await AddTicket(TicketStatus.Open, TicketPriority.Unassigned, DateTime.UtcNow, null);

            var result = await _controller.Index();

            var view = (ViewResult)result;
            var model = view.Model as DashboardViewModel;

            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.HighPriorityCount);
            Assert.AreEqual(1, model.MediumPriorityCount);
            Assert.AreEqual(0, model.LowPriorityCount);
            Assert.AreEqual(1, model.UnassignedPriorityCount);
        }

        [TestMethod]
        public async Task Index_ShouldIdentifyTicketOverdueForTriage()
        {
            // Created well over the 2-business-hour triage target, never triaged.
            await AddTicket(
                TicketStatus.Open,
                TicketPriority.Unassigned,
                DateTime.UtcNow.AddDays(-5),
                null);

            var result = await _controller.Index();

            var view = (ViewResult)result;
            var model = view.Model as DashboardViewModel;

            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.OverdueTickets.Count);
            Assert.AreEqual("Triage", model.OverdueTickets[0].OverdueSla);
        }

        [TestMethod]
        public async Task Index_ShouldIdentifyTicketOverdueForResolution()
        {
            // Triaged well over the High priority 8-business-hour resolution target.
            await AddTicket(
                TicketStatus.InProgress,
                TicketPriority.High,
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(-5));

            var result = await _controller.Index();

            var view = (ViewResult)result;
            var model = view.Model as DashboardViewModel;

            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.OverdueTickets.Count);
            Assert.AreEqual("Resolution", model.OverdueTickets[0].OverdueSla);
        }

        [TestMethod]
        public async Task Index_ShouldExcludeResolvedTicketsFromOverdueList()
        {
            // Even though timestamps would otherwise breach SLA, resolved tickets
            // are already complete and should not appear as overdue.
            await AddTicket(
                TicketStatus.Resolved,
                TicketPriority.High,
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(-4));

            var result = await _controller.Index();

            var view = (ViewResult)result;
            var model = view.Model as DashboardViewModel;

            Assert.IsNotNull(model);
            Assert.AreEqual(0, model.OverdueTickets.Count);
        }

        [TestMethod]
        public async Task Index_NoOverdueTickets_ShouldReturnEmptyOverdueList()
        {
            // Recently created and recently triaged - well within SLA targets.
            await AddTicket(
                TicketStatus.InProgress,
                TicketPriority.High,
                DateTime.UtcNow,
                DateTime.UtcNow);

            var result = await _controller.Index();

            var view = (ViewResult)result;
            var model = view.Model as DashboardViewModel;

            Assert.IsNotNull(model);
            Assert.AreEqual(0, model.OverdueTickets.Count);
        }

        [TestMethod]
        public async Task Index_OverdueTicket_ShouldIncludeAssignedTechnicianAndPriority()
        {
            await AddTicket(
                TicketStatus.InProgress,
                TicketPriority.Medium,
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(-5),
                assignedTechnician: "Shikha Sindhu");

            var result = await _controller.Index();

            var view = (ViewResult)result;
            var model = view.Model as DashboardViewModel;

            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.OverdueTickets.Count);
            Assert.AreEqual("Shikha Sindhu", model.OverdueTickets[0].AssignedTechnician);
            Assert.AreEqual(TicketPriority.Medium, model.OverdueTickets[0].Priority);
        }

        private async Task<Ticket> AddTicket(
            TicketStatus status,
            TicketPriority priority,
            DateTime createdAt,
            DateTime? triagedAt,
            DateTime? resolvedAt = null,
            string? assignedTechnician = null)
        {
            var ticket = new Ticket
            {
                RequesterName = "Test Requester",
                RequesterEmail = "requester@example.com",
                Title = "Test ticket",
                Description = "Test description",
                Status = status,
                Priority = priority,
                CreatedAt = createdAt,
                TriagedAt = triagedAt,
                ResolvedAt = resolvedAt,
                AssignedTechnician = assignedTechnician
                    ?? (triagedAt.HasValue ? "Alex" : null)
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return ticket;
        }
    }
}