using ITHelpdeskSystem.Models;

namespace ITHelpdeskSystem.ViewModels
{
    public class TicketDetailsViewModel
    {
        public Ticket Ticket { get; set; } = null!;
        // display-friendly NZ local timestamps (derived from stored UTC)
        public DateTime CreatedAtNz { get; set; }
        public DateTime? TriagedAtNz { get; set; }
        public DateTime? ResolvedAtNz { get; set; }

        public DateTime TriageDueAt { get; set; }

        public string TriageSlaStatus { get; set; } = string.Empty;

        public DateTime? ResolutionDueAt { get; set; }

        public string ResolutionSlaStatus { get; set; } = string.Empty;
    }
}
