using ITHelpdeskSystem.Models;

namespace ITHelpdeskSystem.Services
{
    // Provides SLA calculations using New Zealand business hours.
    public class SlaService
    {
        private const int BusinessStartHour = 9;
        private const int BusinessEndHour = 17;

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

        // Calculates an SLA due date using Monday-Friday, 9am-5pm business hours.
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
                return current.Date.AddHours(BusinessStartHour);
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
    }
}