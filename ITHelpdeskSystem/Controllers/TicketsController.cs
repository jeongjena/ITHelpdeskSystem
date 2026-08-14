using ITHelpdeskSystem.Data;
using ITHelpdeskSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // Displays full read-only details for a single ticket, regardless of status.
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // Displays all submitted tickets.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tickets = await _context.Tickets
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tickets);
        }

        // Displays the selected ticket for triage.
        [HttpGet]
        public async Task<IActionResult> Triage(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            // Only Open tickets can be triaged.
            if (ticket.Status != TicketStatus.Open)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(ticket);
        }

        // Processes the completed ticket triage.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Triage(
            int id,
            TicketPriority priority,
            string? assignedTechnician)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            // A ticket cannot complete triage without a priority.
            if (priority == TicketPriority.Unassigned)
            {
                ModelState.AddModelError(
                    "Priority",
                    "Please select a priority.");
            }

            // A technician must be assigned before triage can be completed.
            if (string.IsNullOrWhiteSpace(assignedTechnician))
            {
                ModelState.AddModelError(
                    "AssignedTechnician",
                    "Please assign a technician.");
            }

            if (!ModelState.IsValid)
            {
                return View(ticket);
            }

            // Complete the triage workflow.
            ticket.Priority = priority;
            ticket.AssignedTechnician = assignedTechnician.Trim();
            ticket.TriagedAt = DateTime.UtcNow;
            ticket.Status = TicketStatus.InProgress;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Displays the selected ticket for resolution.
        [HttpGet]
        public async Task<IActionResult> Resolve(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            // Only InProgress tickets can be resolved.
            if (ticket.Status != TicketStatus.InProgress)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(ticket);
        }

        // Processes the completed ticket resolution.
        [HttpPost]
        [ActionName("Resolve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveConfirmed(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            // Only InProgress tickets can be resolved.
            if (ticket.Status != TicketStatus.InProgress)
            {
                return RedirectToAction(nameof(Index));
            }

            // Complete the resolution workflow.
            ticket.Status = TicketStatus.Resolved;
            ticket.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}
