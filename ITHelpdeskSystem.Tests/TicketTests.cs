using ITHelpdeskSystem.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ITHelpdeskSystem.Tests
{
    [TestClass]
    public class TicketTests
    {
        [TestMethod]
        public void TicketPriority_ShouldHaveFourValues_InCorrectOrder()
        {
            // Checks the enum values are exactly what we expect, in order
            Assert.AreEqual(0, (int)TicketPriority.Unassigned);
            Assert.AreEqual(1, (int)TicketPriority.Low);
            Assert.AreEqual(2, (int)TicketPriority.Medium);
            Assert.AreEqual(3, (int)TicketPriority.High);
        }

        [TestMethod]
        public void TicketStatus_ShouldHaveThreeValues_InCorrectOrder()
        {
            Assert.AreEqual(0, (int)TicketStatus.Open);
            Assert.AreEqual(1, (int)TicketStatus.InProgress);
            Assert.AreEqual(2, (int)TicketStatus.Resolved);
        }

        [TestMethod]
        public void NewTicket_DefaultPriorityAndStatus_ShouldMatchFR04()
        {
            // FR-04 says a new ticket should default to Status = Open, Priority = Unassigned
            var ticket = new Ticket();

            Assert.AreEqual(TicketStatus.Open, ticket.Status);
            Assert.AreEqual(TicketPriority.Unassigned, ticket.Priority);
            Assert.IsNull(ticket.AssignedTechnician);
            Assert.IsNull(ticket.TriagedAt);
            Assert.IsNull(ticket.ResolvedAt);
        }

        [TestMethod]
        public void Ticket_ShouldStoreAssignedValues_Correctly()
        {
            // Checks that when we set values on a ticket, they come back correctly
            var ticket = new Ticket
            {
                RequesterName = "Aarti",
                RequesterEmail = "aarti@example.com",
                Title = "Laptop not turning on",
                Description = "Screen stays black on power up",
                Priority = TicketPriority.High,
                Status = TicketStatus.InProgress
            };

            Assert.AreEqual("Aarti", ticket.RequesterName);
            Assert.AreEqual("aarti@example.com", ticket.RequesterEmail);
            Assert.AreEqual("Laptop not turning on", ticket.Title);
            Assert.AreEqual(TicketPriority.High, ticket.Priority);
            Assert.AreEqual(TicketStatus.InProgress, ticket.Status);
        }

        [TestMethod]
        public void Ticket_MissingRequiredFields_ShouldFailValidation()
        {
            var ticket = new Ticket(); // nothing filled in

            var context = new ValidationContext(ticket);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(ticket, context, results, true);

            Assert.IsFalse(isValid);
            Assert.IsTrue(results.Count > 0);
        }

        [TestMethod]
        public void Ticket_InvalidEmailFormat_ShouldFailValidation()
        {
            var ticket = new Ticket
            {
                RequesterName = "Aarti",
                RequesterEmail = "not-an-email",
                Title = "Test",
                Description = "Test description"
            };

            var context = new ValidationContext(ticket);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(ticket, context, results, true);

            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void Ticket_ValidData_ShouldPassValidation()
        {
            var ticket = new Ticket
            {
                RequesterName = "Aarti",
                RequesterEmail = "aarti@example.com",
                Title = "Laptop not turning on",
                Description = "Screen stays black on power up"
            };

            var context = new ValidationContext(ticket);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(ticket, context, results, true);

            Assert.IsTrue(isValid);
        }
    }

}

