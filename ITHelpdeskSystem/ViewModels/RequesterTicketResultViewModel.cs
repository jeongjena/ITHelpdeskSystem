using ITHelpdeskSystem.Models;

namespace ITHelpdeskSystem.ViewModels
{
    public class RequesterTicketResultViewModel
    {
        public int TicketId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TicketStatus Status { get; set; }
        public TicketPriority Priority { get; set; }
        public DateTime CreatedAtNz { get; set; }
    }
}
