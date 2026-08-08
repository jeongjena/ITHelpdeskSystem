using ITHelpdeskSystem.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;
using ITHelpdeskSystem.Models;

namespace ITHelpdeskSystem.Tests
{
    [TestClass]
    public class TicketTests
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
        public void Ticket_MissingRequiredFields_ShouldFailValidation()
        {
            var ticket = new Ticket();

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

        [TestMethod]
        public void Ticket_TitleExceedsMaxLength_ShouldFailValidation()
        {
            var ticket = new Ticket
            {
                RequesterName = "Aarti",
                RequesterEmail = "aarti@example.com",
                Title = new string('a', 201),
                Description = "Valid description"
            };

            var context = new ValidationContext(ticket);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(ticket, context, results, true);

            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void Ticket_DescriptionExceedsMaxLength_ShouldFailValidation()
        {
            var ticket = new Ticket
            {
                RequesterName = "Aarti",
                RequesterEmail = "aarti@example.com",
                Title = "Valid title",
                Description = new string('a', 2001)
            };

            var context = new ValidationContext(ticket);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(ticket, context, results, true);

            Assert.IsFalse(isValid);
        }
    }
}