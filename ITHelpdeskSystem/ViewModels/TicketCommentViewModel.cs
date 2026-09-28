namespace ITHelpdeskSystem.ViewModels
{
    public class TicketCommentViewModel
    {
        public string Text { get; set; } = string.Empty;

        // Display-friendly NZ local timestamp converted from stored UTC.
        public DateTime CreatedAtNz { get; set; }
    }
}
