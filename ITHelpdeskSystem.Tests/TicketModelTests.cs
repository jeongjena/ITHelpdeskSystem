using ITHelpdeskSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ITHelpdeskSystem.Tests
{
    [TestClass]
    public class TicketModelTests
    {
        [TestMethod]
        public void NewTicket_ShouldUseDefaultWorkflowValues()
        {
            var ticket = new Ticket();

            Assert.AreEqual(TicketStatus.Open, ticket.Status);
            Assert.AreEqual(TicketPriority.Unassigned, ticket.Priority);
            Assert.IsNull(ticket.AssignedTechnician);
            Assert.IsNull(ticket.TriagedAt);
            Assert.IsNull(ticket.ResolvedAt);
        }

        [TestMethod]
        public void Ticket_MissingRequiredFields_ShouldReturnValidationErrors()
        {
            var ticket = new Ticket();

            var results = Validate(ticket);

            Assert.IsTrue(
                results.Any(r =>
                    r.MemberNames.Contains(nameof(Ticket.RequesterName))));

            Assert.IsTrue(
                results.Any(r =>
                    r.MemberNames.Contains(nameof(Ticket.RequesterEmail))));

            Assert.IsTrue(
                results.Any(r =>
                    r.MemberNames.Contains(nameof(Ticket.Title))));

            Assert.IsTrue(
                results.Any(r =>
                    r.MemberNames.Contains(nameof(Ticket.Description))));
        }

        [TestMethod]
        public void Ticket_InvalidEmailFormat_ShouldReturnValidationError()
        {
            var ticket = CreateValidTicket();
            ticket.RequesterEmail = "not-an-email";

            var results = Validate(ticket);

            Assert.IsTrue(
                results.Any(r =>
                    r.MemberNames.Contains(nameof(Ticket.RequesterEmail))));
        }

        [TestMethod]
        public void Ticket_ValidData_ShouldPassValidation()
        {
            var ticket = CreateValidTicket();

            var results = Validate(ticket);

            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Ticket_TitleExceedsMaximumLength_ShouldFailValidation()
        {
            var ticket = CreateValidTicket();
            ticket.Title = new string('a', 201);

            var results = Validate(ticket);

            Assert.IsTrue(
                results.Any(r =>
                    r.MemberNames.Contains(nameof(Ticket.Title))));
        }

        [TestMethod]
        public void Ticket_DescriptionExceedsMaximumLength_ShouldFailValidation()
        {
            var ticket = CreateValidTicket();
            ticket.Description = new string('a', 2001);

            var results = Validate(ticket);

            Assert.IsTrue(
                results.Any(r =>
                    r.MemberNames.Contains(nameof(Ticket.Description))));
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

        private static List<ValidationResult> Validate(Ticket ticket)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(ticket);

            Validator.TryValidateObject(
                ticket,
                context,
                results,
                validateAllProperties: true);

            return results;
        }
    }
}