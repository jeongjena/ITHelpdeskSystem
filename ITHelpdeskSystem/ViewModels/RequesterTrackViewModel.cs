using System.ComponentModel.DataAnnotations;

namespace ITHelpdeskSystem.ViewModels
{
    public class RequesterTrackViewModel
    {
        [Required(ErrorMessage = "Ticket ID is required.")]
        [Display(Name = "Ticket ID")]
        public int? TicketId { get; set; }

        [Required(ErrorMessage = "Requester email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256)]
        [Display(Name = "Requester Email")]
        public string RequesterEmail { get; set; } = string.Empty;
    }
}