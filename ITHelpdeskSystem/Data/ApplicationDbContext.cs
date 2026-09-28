using ITHelpdeskSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ITHelpdeskSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Ticket> Tickets { get; set; }

        // Stores progress comments linked to support tickets.
        public DbSet<TicketComment> TicketComments { get; set; }
    }
}
