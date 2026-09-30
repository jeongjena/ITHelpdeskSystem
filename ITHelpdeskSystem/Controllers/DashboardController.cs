using ITHelpdeskSystem.Data;
using ITHelpdeskSystem.Services;
using ITHelpdeskSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult Index()
        {
            var viewModel = new DashboardViewModel();

            return View(viewModel);
        }
    }
}