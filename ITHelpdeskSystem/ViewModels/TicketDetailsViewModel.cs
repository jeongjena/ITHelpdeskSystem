using ITHelpdeskSystem.Models;

namespace ITHelpdeskSystem.ViewModels
{
    public class TicketDetailsViewModel
    {
        public Ticket Ticket { get; set; } = null!;

        public DateTime TriageDueAt { get; set; }

        public string TriageSlaStatus { get; set; } = string.Empty;

        public DateTime? ResolutionDueAt { get; set; }

        public string ResolutionSlaStatus { get; set; } = string.Empty;
    }
}
