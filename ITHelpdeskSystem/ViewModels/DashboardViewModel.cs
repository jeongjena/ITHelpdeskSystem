using ITHelpdeskSystem.Models;

namespace ITHelpdeskSystem.ViewModels
{
    // Aggregated data for the staff dashboard (FR-22 to FR-24).
    public class DashboardViewModel
    {
        // Ticket counts grouped by workflow status.
        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }

        // Ticket counts grouped by priority.
        public int HighPriorityCount { get; set; }
        public int MediumPriorityCount { get; set; }
        public int LowPriorityCount { get; set; }
        public int UnassignedPriorityCount { get; set; }

        // Tickets currently overdue for triage or resolution.
        public List<OverdueTicketViewModel> OverdueTickets { get; set; }
            = new List<OverdueTicketViewModel>();
    }

    // Represents a single ticket overdue for triage or resolution.
    public class OverdueTicketViewModel
    {
        public int TicketId { get; set; }
        public string Title { get; set; } = string.Empty;
        public TicketStatus Status { get; set; }
        public TicketPriority Priority { get; set; }
        public string? AssignedTechnician { get; set; }

        // Indicates which SLA is overdue: "Triage" or "Resolution".
        public string OverdueSla { get; set; } = string.Empty;
    }
}