using System.ComponentModel.DataAnnotations;

namespace ITHelpdeskSystem.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        public string RequesterName { get; set; }

        [Required]
        [EmailAddress]
        public string RequesterEmail { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public TicketPriority Priority { get; set; }
        public TicketStatus Status { get; set; }
        public string? AssignedTechnician { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? TriagedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
