namespace ITHelpdeskSystem.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string RequesterName { get; set; }
        public string RequesterEmail { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TicketPriority Priority { get; set; }
        public TicketStatus Status { get; set; }
        public string? AssignedTechnician { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

    }
}
