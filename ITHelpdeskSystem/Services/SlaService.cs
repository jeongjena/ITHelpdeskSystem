using ITHelpdeskSystem.Models;

namespace ITHelpdeskSystem.Services
{
    // Provides SLA calculations using New Zealand business hours.
    public class SlaService
    {
        private const int BusinessStartHour = 9;
        private const int BusinessEndHour = 17;

        private readonly TimeZoneInfo _newZealandTimeZone;

        public SlaService()
        {
            _newZealandTimeZone = GetNewZealandTimeZone();
        }

        // Returns the resolution target in business hours
        // based on the assigned ticket priority.
        public int GetResolutionTargetHours(TicketPriority priority)
        {
            return priority switch
            {
                TicketPriority.High => 8,
                TicketPriority.Medium => 16,
                TicketPriority.Low => 40,
                _ => 0
            };
        }

        // Converts a UTC timestamp stored in the database
        // to New Zealand local time.
        public DateTime ConvertUtcToNewZealandTime(DateTime utcDateTime)
        {
            // SQLite may return DateTime values with Kind = Unspecified,
            // so treat stored timestamps as UTC explicitly.
            var utc = DateTime.SpecifyKind(
                utcDateTime,
                DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(
                utc,
                _newZealandTimeZone);
        }

        // Calculates an SLA due date using a New Zealand local time.
        // Business hours are Monday-Friday, 9am-5pm.
        public DateTime CalculateDueDate(
            DateTime startTime,
            int businessHours)
        {
            var current = MoveToBusinessTime(startTime);
            var remainingHours = businessHours;

            while (remainingHours > 0)
            {
                var endOfBusinessDay =
                    current.Date.AddHours(BusinessEndHour);

                var availableHours =
                    (endOfBusinessDay - current).TotalHours;

                if (remainingHours <= availableHours)
                {
                    return current.AddHours(remainingHours);
                }

                remainingHours -= (int)availableHours;

                // Move to 9am on the next business day.
                current = MoveToBusinessTime(
                    current.Date.AddDays(1)
                        .AddHours(BusinessStartHour));
            }

            return current;
        }

        // Calculates a due date from a UTC timestamp stored in the database.
        public DateTime CalculateDueDateFromUtc(
            DateTime utcStartTime,
            int businessHours)
        {
            var nzStartTime =
                ConvertUtcToNewZealandTime(utcStartTime);

            return CalculateDueDate(
                nzStartTime,
                businessHours);
        }

        // Returns the current triage SLA status for a ticket.
        public string GetTriageSlaStatus(
            Ticket ticket,
            DateTime currentTimeUtc)
        {
            var createdAtNz =
                ConvertUtcToNewZealandTime(ticket.CreatedAt);

            var currentTimeNz =
                ConvertUtcToNewZealandTime(currentTimeUtc);

            var dueAt =
                CalculateDueDate(createdAtNz, 2);

            if (ticket.TriagedAt.HasValue)
            {
                var triagedAtNz =
                    ConvertUtcToNewZealandTime(
                        ticket.TriagedAt.Value);

                return triagedAtNz <= dueAt
                    ? "Met"
                    : "Breached";
            }

            return currentTimeNz <= dueAt
                ? "Within SLA"
                : "Overdue";
        }

        // Returns the current resolution SLA status for a ticket.
        public string GetResolutionSlaStatus(
            Ticket ticket,
            DateTime currentTimeUtc)
        {
            // Resolution SLA begins only after triage.
            if (!ticket.TriagedAt.HasValue ||
                ticket.Priority == TicketPriority.Unassigned)
            {
                return "Not Started";
            }

            var targetHours =
                GetResolutionTargetHours(ticket.Priority);

            var triagedAtNz =
                ConvertUtcToNewZealandTime(
                    ticket.TriagedAt.Value);

            var currentTimeNz =
                ConvertUtcToNewZealandTime(
                    currentTimeUtc);

            var dueAt =
                CalculateDueDate(
                    triagedAtNz,
                    targetHours);

            if (ticket.ResolvedAt.HasValue)
            {
                var resolvedAtNz =
                    ConvertUtcToNewZealandTime(
                        ticket.ResolvedAt.Value);

                return resolvedAtNz <= dueAt
                    ? "Met"
                    : "Breached";
            }

            return currentTimeNz <= dueAt
                ? "Within SLA"
                : "Overdue";
        }

        // Moves a date and time to the next valid business time.
        private DateTime MoveToBusinessTime(DateTime dateTime)
        {
            var current = dateTime;

            // Move weekends to Monday.
            while (current.DayOfWeek == DayOfWeek.Saturday ||
                   current.DayOfWeek == DayOfWeek.Sunday)
            {
                current = current.Date
                    .AddDays(1)
                    .AddHours(BusinessStartHour);
            }

            // Before 9am, start at 9am on the same business day.
            if (current.Hour < BusinessStartHour)
            {
                return current.Date
                    .AddHours(BusinessStartHour);
            }

            // At or after 5pm, move to 9am on the next business day.
            if (current.Hour >= BusinessEndHour)
            {
                current = current.Date
                    .AddDays(1)
                    .AddHours(BusinessStartHour);

                return MoveToBusinessTime(current);
            }

            return current;
        }

        // Retrieves the New Zealand time zone.
        private static TimeZoneInfo GetNewZealandTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "Pacific/Auckland");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback for Windows environments that use Windows time zone IDs.
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "New Zealand Standard Time");
            }
        }
    }
}