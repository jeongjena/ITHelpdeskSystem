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
    }
}
