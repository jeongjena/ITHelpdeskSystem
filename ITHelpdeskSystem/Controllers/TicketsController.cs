using ITHelpdeskSystem.Data;
using ITHelpdeskSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace ITHelpdeskSystem.Controllers
{
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Receives the database connection provided by the system.
        public TicketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Displays the empty ticket submission form.
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Processes the submitted ticket form.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ticket ticket)
        {
            if (!ModelState.IsValid)
            {
                return View(ticket);
            }

            ticket.Priority = TicketPriority.Unassigned;
            ticket.Status = TicketStatus.Open;
            ticket.AssignedTechnician = null;
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.TriagedAt = null;
            ticket.ResolvedAt = null;

            // Save the new ticket to the SQLite database.
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Redirect to the confirmation page using the generated ticket ID.
            return RedirectToAction(nameof(Confirmation), new { id = ticket.Id });
        }

        // Displays confirmation details for the newly created ticket.
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }
    }
}
