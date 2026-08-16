using ITHelpdeskSystem.Models;
using ITHelpdeskSystem.Services;

namespace ITHelpdeskSystem.Tests
{
    [TestClass]
    public class SlaServiceTests
    {
        private SlaService _slaService = null!;

        [TestInitialize]
        public void Setup()
        {
            _slaService = new SlaService();
        }

        [TestMethod]
        public void GetResolutionTargetHours_ShouldReturnCorrectHours()
        {
            Assert.AreEqual(
                8,
                _slaService.GetResolutionTargetHours(TicketPriority.High));

            Assert.AreEqual(
                16,
                _slaService.GetResolutionTargetHours(TicketPriority.Medium));

            Assert.AreEqual(
                40,
                _slaService.GetResolutionTargetHours(TicketPriority.Low));
        }

        [TestMethod]
        public void CalculateDueDate_DuringBusinessHours_ShouldAddHoursNormally()
        {
            // Monday 10am + 2 business hours = Monday 12pm.
            var start = new DateTime(2026, 8, 17, 10, 0, 0);

            var result = _slaService.CalculateDueDate(start, 2);

            var expected = new DateTime(2026, 8, 17, 12, 0, 0);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CalculateDueDate_CrossingEndOfDay_ShouldContinueNextMorning()
        {
            // Monday 4pm + 2 business hours = Tuesday 10am.
            var start = new DateTime(2026, 8, 17, 16, 0, 0);

            var result = _slaService.CalculateDueDate(start, 2);

            var expected = new DateTime(2026, 8, 18, 10, 0, 0);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CalculateDueDate_CrossingWeekend_ShouldContinueOnMonday()
        {
            // Friday 4pm + 2 business hours = Monday 10am.
            var start = new DateTime(2026, 8, 21, 16, 0, 0);

            var result = _slaService.CalculateDueDate(start, 2);

            var expected = new DateTime(2026, 8, 24, 10, 0, 0);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CalculateDueDate_StartingOnWeekend_ShouldStartMondayMorning()
        {
            // Sunday 10:30pm starts the SLA clock on Monday at 9am.
            var start = new DateTime(2026, 8, 16, 22, 30, 0);

            var result = _slaService.CalculateDueDate(start, 2);

            var expected = new DateTime(2026, 8, 17, 11, 0, 0);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CalculateDueDate_BeforeBusinessHours_ShouldStartAt9am()
        {
            // Monday 8am starts the SLA clock at 9am.
            var start = new DateTime(2026, 8, 17, 8, 0, 0);

            var result = _slaService.CalculateDueDate(start, 2);

            var expected = new DateTime(2026, 8, 17, 11, 0, 0);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CalculateDueDate_AtBusinessStart_ShouldCalculateNormally()
        {
            // Monday 9am + 2 business hours = Monday 11am.
            var start = new DateTime(2026, 8, 17, 9, 0, 0);

            var result = _slaService.CalculateDueDate(start, 2);

            var expected = new DateTime(2026, 8, 17, 11, 0, 0);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CalculateDueDate_AtBusinessEnd_ShouldStartNextBusinessDay()
        {
            // Monday 5pm starts the SLA clock on Tuesday at 9am.
            var start = new DateTime(2026, 8, 17, 17, 0, 0);

            var result = _slaService.CalculateDueDate(start, 2);

            var expected = new DateTime(2026, 8, 18, 11, 0, 0);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void GetTriageSlaStatus_OpenTicketBeforeDue_ShouldReturnWithinSla()
        {
            // Monday 9am NZST = Sunday 9pm UTC.
            var ticket = new Ticket
            {
                CreatedAt = new DateTime(
                    2026, 8, 16, 21, 0, 0,
                    DateTimeKind.Utc)
            };

            // Monday 10am NZST = Sunday 10pm UTC.
            var currentTime = new DateTime(
                2026, 8, 16, 22, 0, 0,
                DateTimeKind.Utc);

            var result =
                _slaService.GetTriageSlaStatus(
                    ticket,
                    currentTime);

            Assert.AreEqual("Within SLA", result);
        }

        [TestMethod]
        public void GetTriageSlaStatus_OpenTicketAfterDue_ShouldReturnOverdue()
        {
            // Monday 9am NZST = Sunday 9pm UTC.
            var ticket = new Ticket
            {
                CreatedAt = new DateTime(
                    2026, 8, 16, 21, 0, 0,
                    DateTimeKind.Utc)
            };

            // Monday 12pm NZST = Monday 12am UTC.
            var currentTime = new DateTime(
                2026, 8, 17, 0, 0, 0,
                DateTimeKind.Utc);

            var result =
                _slaService.GetTriageSlaStatus(
                    ticket,
                    currentTime);

            Assert.AreEqual("Overdue", result);
        }

        [TestMethod]
        public void GetTriageSlaStatus_TriagedInTime_ShouldReturnMet()
        {
            // Monday 9am NZST = Sunday 9pm UTC.
            var ticket = new Ticket
            {
                CreatedAt = new DateTime(
                    2026, 8, 16, 21, 0, 0,
                    DateTimeKind.Utc),

                // Monday 10:30am NZST = Sunday 10:30pm UTC.
                TriagedAt = new DateTime(
                    2026, 8, 16, 22, 30, 0,
                    DateTimeKind.Utc)
            };

            var currentTime = new DateTime(
                2026, 8, 17, 0, 0, 0,
                DateTimeKind.Utc);

            var result =
                _slaService.GetTriageSlaStatus(
                    ticket,
                    currentTime);

            Assert.AreEqual("Met", result);
        }

        [TestMethod]
        public void GetTriageSlaStatus_TriagedLate_ShouldReturnBreached()
        {
            // Monday 9am NZST = Sunday 9pm UTC.
            var ticket = new Ticket
            {
                CreatedAt = new DateTime(
                    2026, 8, 16, 21, 0, 0,
                    DateTimeKind.Utc),

                // Monday 12pm NZST = Monday 12am UTC.
                TriagedAt = new DateTime(
                    2026, 8, 17, 0, 0, 0,
                    DateTimeKind.Utc)
            };

            var currentTime = new DateTime(
                2026, 8, 17, 0, 0, 0,
                DateTimeKind.Utc);

            var result =
                _slaService.GetTriageSlaStatus(
                    ticket,
                    currentTime);

            Assert.AreEqual("Breached", result);
        }

        [TestMethod]
        public void GetResolutionSlaStatus_BeforeTriage_ShouldReturnNotStarted()
        {
            var ticket = new Ticket
            {
                CreatedAt = new DateTime(
                    2026, 8, 16, 21, 0, 0,
                    DateTimeKind.Utc),

                Priority = TicketPriority.Unassigned
            };

            var currentTime = new DateTime(
                2026, 8, 16, 22, 0, 0,
                DateTimeKind.Utc);

            var result =
                _slaService.GetResolutionSlaStatus(
                    ticket,
                    currentTime);

            Assert.AreEqual("Not Started", result);
        }

        [TestMethod]
        public void GetResolutionSlaStatus_InProgressBeforeDue_ShouldReturnWithinSla()
        {
            var ticket = new Ticket
            {
                Priority = TicketPriority.High,

                // Monday 9am NZST = Sunday 9pm UTC.
                TriagedAt = new DateTime(
                    2026, 8, 16, 21, 0, 0,
                    DateTimeKind.Utc)
            };

            // Monday 3pm NZST = Monday 3am UTC.
            var currentTime = new DateTime(
                2026, 8, 17, 3, 0, 0,
                DateTimeKind.Utc);

            var result =
                _slaService.GetResolutionSlaStatus(
                    ticket,
                    currentTime);

            Assert.AreEqual("Within SLA", result);
        }

        [TestMethod]
        public void GetResolutionSlaStatus_InProgressAfterDue_ShouldReturnOverdue()
        {
            var ticket = new Ticket
            {
                Priority = TicketPriority.High,

                // Monday 9am NZST.
                TriagedAt = new DateTime(
                    2026, 8, 16, 21, 0, 0,
                    DateTimeKind.Utc)
            };

            // High priority = 8 business hours.
            // Due Monday 5pm NZST.
            // Tuesday 9am NZST is therefore overdue.
            var currentTime = new DateTime(
                2026, 8, 17, 21, 0, 0,
                DateTimeKind.Utc);

            var result =
                _slaService.GetResolutionSlaStatus(
                    ticket,
                    currentTime);

            Assert.AreEqual("Overdue", result);
        }

        [TestMethod]
        public void GetResolutionSlaStatus_ResolvedInTime_ShouldReturnMet()
        {
            var ticket = new Ticket
            {
                Priority = TicketPriority.High,

                // Monday 9am NZST.
                TriagedAt = new DateTime(
                    2026, 8, 16, 21, 0, 0,
                    DateTimeKind.Utc),

                // Monday 4pm NZST = Monday 4am UTC.
                ResolvedAt = new DateTime(
                    2026, 8, 17, 4, 0, 0,
                    DateTimeKind.Utc)
            };

            var currentTime = new DateTime(
                2026, 8, 17, 21, 0, 0,
                DateTimeKind.Utc);

            var result =
                _slaService.GetResolutionSlaStatus(
                    ticket,
                    currentTime);

            Assert.AreEqual("Met", result);
        }

        [TestMethod]
        public void GetResolutionSlaStatus_ResolvedLate_ShouldReturnBreached()
        {
            var ticket = new Ticket
            {
                Priority = TicketPriority.High,

                // Monday 9am NZST.
                TriagedAt = new DateTime(
                    2026, 8, 16, 21, 0, 0,
                    DateTimeKind.Utc),

                // Tuesday 10am NZST = Monday 10pm UTC.
                ResolvedAt = new DateTime(
                    2026, 8, 17, 22, 0, 0,
                    DateTimeKind.Utc)
            };

            var currentTime = new DateTime(
                2026, 8, 17, 22, 0, 0,
                DateTimeKind.Utc);

            var result =
                _slaService.GetResolutionSlaStatus(
                    ticket,
                    currentTime);

            Assert.AreEqual("Breached", result);
        }

        [TestMethod]
        public void ConvertUtcToNewZealandTime_InWinter_ShouldUseNzst()
        {
            // 1 August 2026 is during NZST (UTC+12).
            var utc = new DateTime(
                2026, 8, 1, 0, 0, 0,
                DateTimeKind.Utc);

            var result =
                _slaService.ConvertUtcToNewZealandTime(utc);

            var expected =
                new DateTime(2026, 8, 1, 12, 0, 0);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ConvertUtcToNewZealandTime_InSummer_ShouldUseNzdt()
        {
            // 1 December 2026 is during NZDT (UTC+13).
            var utc = new DateTime(
                2026, 12, 1, 0, 0, 0,
                DateTimeKind.Utc);

            var result =
                _slaService.ConvertUtcToNewZealandTime(utc);

            var expected =
                new DateTime(2026, 12, 1, 13, 0, 0);

            Assert.AreEqual(expected, result);
        }
    }
}