using System.ComponentModel.DataAnnotations;

namespace ITHelpdeskSystem.Models
{
    public class TicketComment
    {
        public int Id { get; set; }

        public int TicketId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public Ticket Ticket { get; set; } = null!;
    }
}
