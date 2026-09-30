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

            var viewModel = new DashboardViewModel
            {
                OpenCount = tickets.Count(t => t.Status == TicketStatus.Open),
                InProgressCount = tickets.Count(t => t.Status == TicketStatus.InProgress),
                ResolvedCount = tickets.Count(t => t.Status == TicketStatus.Resolved),

                HighPriorityCount = tickets.Count(t => t.Priority == TicketPriority.High),
                MediumPriorityCount = tickets.Count(t => t.Priority == TicketPriority.Medium),
                LowPriorityCount = tickets.Count(t => t.Priority == TicketPriority.Low),
                UnassignedPriorityCount = tickets.Count(t => t.Priority == TicketPriority.Unassigned)
            };

            return View(viewModel);
        }
    }
}