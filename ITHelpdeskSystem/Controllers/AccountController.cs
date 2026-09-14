using ITHelpdeskSystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ITHelpdeskSystem.Controllers
{
    public class AccountController : Controller
    {
        // Displays the IT staff login form.
        [HttpGet]
        public IActionResult Login()
        {
            // If the user is already logged in, go directly to Ticket Management.
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Tickets");
            }

            return View();

        }

        // Processes the submitted IT staff login form.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Return the form with validation errors if required fields are missing.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Basic staff credentials for the semester prototype.
            // These can be moved to configuration later to avoid storing
            // credentials directly in the controller.
            if (model.Username == "staff" &&
                model.Password == "helpdesk123")
            {
                // Store the authenticated staff user's identity and role.
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Username),
                    new Claim(ClaimTypes.Role, "ITStaff")
                };

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var principal = new ClaimsPrincipal(identity);

                // Create the authentication cookie for the logged-in staff user.
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal);

                // After successful login, go to Ticket Management.
                return RedirectToAction("Index", "Tickets");
            }

            // Do not reveal whether the username or password was incorrect.
            ModelState.AddModelError(
                string.Empty,
                "Invalid username or password.");

            return View(model);
        }

        // Logs the current IT staff user out.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Remove the authentication cookie.
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }
    }
}