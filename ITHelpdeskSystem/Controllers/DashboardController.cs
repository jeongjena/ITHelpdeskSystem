using ITHelpdeskSystem.Data;
using ITHelpdeskSystem.Models;
using ITHelpdeskSystem.Services;
using ITHelpdeskSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITHelpdeskSystem.Controllers
{
    // Staff-only dashboard summarising ticket workload (FR-22 to FR-24).
    [Authorize(Roles = "ITStaff")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SlaService _slaService;

        public DashboardController(
            ApplicationDbContext context,
            SlaService slaService)
        {
            _context = context;
            _slaService = slaService;
        }

        // Displays the staff dashboard.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tickets = await _context.Tickets.ToListAsync();
            var currentTime = DateTime.UtcNow;

            var viewModel = new DashboardViewModel
            {
                OpenCount = tickets.Count(t => t.Status == TicketStatus.Open),
                InProgressCount = tickets.Count(t => t.Status == TicketStatus.InProgress),
                ResolvedCount = tickets.Count(t => t.Status == TicketStatus.Resolved),

                HighPriorityCount = tickets.Count(t => t.Priority == TicketPriority.High),
                MediumPriorityCount = tickets.Count(t => t.Priority == TicketPriority.Medium),
                LowPriorityCount = tickets.Count(t => t.Priority == TicketPriority.Low),
                UnassignedPriorityCount = tickets.Count(t => t.Priority == TicketPriority.Unassigned),

                OverdueTickets = BuildOverdueTickets(tickets, currentTime)
            };

            return View(viewModel);
        }

        // Identifies tickets currently overdue for triage or resolution,
        // reusing the existing SlaService rather than duplicating SLA logic.
        private List<OverdueTicketViewModel> BuildOverdueTickets(
            List<Ticket> tickets,
            DateTime currentTime)
        {
            var overdueTickets = new List<OverdueTicketViewModel>();

            foreach (var ticket in tickets)
            {
                // Skip tickets that are already resolved — nothing to escalate.
                if (ticket.Status == TicketStatus.Resolved)
                {
                    continue;
                }

                var triageStatus = _slaService.GetTriageSlaStatus(ticket, currentTime);

                if (triageStatus == "Overdue")
                {
                    overdueTickets.Add(new OverdueTicketViewModel
                    {
                        TicketId = ticket.Id,
                        Title = ticket.Title,
                        Status = ticket.Status,
                        Priority = ticket.Priority,
                        AssignedTechnician = ticket.AssignedTechnician,
                        OverdueSla = "Triage"
                    });
                    continue;
                }

                var resolutionStatus = _slaService.GetResolutionSlaStatus(ticket, currentTime);

                if (resolutionStatus == "Overdue")
                {
                    overdueTickets.Add(new OverdueTicketViewModel
                    {
                        TicketId = ticket.Id,
                        Title = ticket.Title,
                        Status = ticket.Status,
                        Priority = ticket.Priority,
                        AssignedTechnician = ticket.AssignedTechnician,
                        OverdueSla = "Resolution"
                    });
                }
            }

            return overdueTickets;
        }
    }
}