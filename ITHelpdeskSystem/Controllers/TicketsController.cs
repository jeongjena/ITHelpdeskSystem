using ITHelpdeskSystem.Data;
using ITHelpdeskSystem.Models;
using ITHelpdeskSystem.Services;
using ITHelpdeskSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ITHelpdeskSystem.Controllers
{
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SlaService _slaService;

        // Receives the database connection provided by the system.
        public TicketsController(
            ApplicationDbContext context,
            SlaService slaService)
        {
            _context = context;
            _slaService = slaService;
        }

        // Public - employees can submit tickets.
        // Displays the empty ticket submission form.
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Public - processes employee ticket submission.
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

        // Public - allows a requester to track an existing ticket by ID and email.
        // Does not require authentication and does not expose whether the ID exists when the email mismatches.
        [HttpGet]
        public IActionResult Track()
        {
            return View(new RequesterTrackViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Track(RequesterTrackViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Ensure TicketId was provided
            if (!model.TicketId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "No matching ticket found.");
                return View(model);
            }

            var ticket = await _context.Tickets.FindAsync(model.TicketId.Value);

            // Generic failure message for both not-found and email mismatch to avoid information disclosure.
            if (ticket == null)
            {
                ModelState.AddModelError(string.Empty, "No matching ticket found.");
                return View(model);
            }

            var providedEmail = model.RequesterEmail?.Trim();
            var storedEmail = ticket.RequesterEmail?.Trim();

            if (string.IsNullOrEmpty(providedEmail) ||
                !string.Equals(providedEmail, storedEmail, System.StringComparison.InvariantCultureIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "No matching ticket found.");
                return View(model);
            }

            var result = new RequesterTicketResultViewModel
            {
                TicketId = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status,
                Priority = ticket.Priority,
                CreatedAtNz = _slaService.ConvertUtcToNewZealandTime(ticket.CreatedAt)
            };

            return View("TrackResult", result);
        }

        // Displays full read-only details for a single ticket, regardless of status.
        [Authorize(Roles = "ITStaff")]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            var currentTime = DateTime.UtcNow;

                var viewModel = new TicketDetailsViewModel
                {
                    Ticket = ticket,

                    // Convert stored UTC timestamps to New Zealand time for display.
                    CreatedAtNz =
                        _slaService.ConvertUtcToNewZealandTime(ticket.CreatedAt),

                    TriagedAtNz =
                        ticket.TriagedAt.HasValue
                            ? _slaService.ConvertUtcToNewZealandTime(ticket.TriagedAt.Value)
                            : null,

                    ResolvedAtNz =
                        ticket.ResolvedAt.HasValue
                            ? _slaService.ConvertUtcToNewZealandTime(ticket.ResolvedAt.Value)
                            : null,

                    TriageDueAt = _slaService.CalculateDueDateFromUtc(
                        ticket.CreatedAt,
                        2),

                    TriageSlaStatus = _slaService.GetTriageSlaStatus(
                        ticket,
                        currentTime),

                    ResolutionSlaStatus = _slaService.GetResolutionSlaStatus(
                        ticket,
                        currentTime)
                };

            // Calculate the resolution SLA due time after valid triage.
            if (ticket.TriagedAt.HasValue &&
                ticket.Priority != TicketPriority.Unassigned)
            {
                var resolutionHours =
                    _slaService.GetResolutionTargetHours(
                        ticket.Priority);

                viewModel.ResolutionDueAt =
                    _slaService.CalculateDueDateFromUtc(
                        ticket.TriagedAt.Value,
                        resolutionHours);
            }

            return View(viewModel);
        }

        // Displays all submitted tickets, optionally filtered by search term, status, and priority.
        [Authorize(Roles = "ITStaff")]
        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm = null,
            TicketStatus? status = null,
            TicketPriority? priority = null)
        {
            var query = _context.Tickets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();

                if (int.TryParse(term, out int ticketId))
                {
                    query = query.Where(t =>
                        t.Id == ticketId ||
                        t.Title.Contains(term) ||
                        t.RequesterName.Contains(term));
                }
                else
                {
                    query = query.Where(t =>
                        t.Title.Contains(term) ||
                        t.RequesterName.Contains(term));
                }
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (priority.HasValue)
            {
                query = query.Where(t => t.Priority == priority.Value);
            }

            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewData["SearchTerm"] = searchTerm;
            ViewData["StatusFilter"] = status;
            ViewData["PriorityFilter"] = priority;

            return View(tickets);
        }

        // Displays the selected ticket for triage.
        [Authorize(Roles = "ITStaff")]
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
        [Authorize(Roles = "ITStaff")]
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

            // Only Open tickets can complete triage.
            if (ticket.Status != TicketStatus.Open)
            {
                return RedirectToAction(nameof(Index));
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
            ticket.AssignedTechnician = assignedTechnician!.Trim();
            ticket.TriagedAt = DateTime.UtcNow;
            ticket.Status = TicketStatus.InProgress;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Displays the selected ticket for resolution.
        [Authorize(Roles = "ITStaff")]
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
        [Authorize(Roles = "ITStaff")]
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
