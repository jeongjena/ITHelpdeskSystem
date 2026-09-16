using Microsoft.VisualStudio.TestTools.UnitTesting;
using ITHelpdeskSystem.Controllers;
using ITHelpdeskSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace ITHelpdeskSystem.Tests
{
    [TestClass]
    public class AccountControllerTests
    {
        [TestMethod]
        public void LoginViewModel_UsernameRequired_ValidationFails()
        {
            var model = new LoginViewModel
            {
                Username = string.Empty,
                Password = "somepassword"
            };

            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(
                model,
                new ValidationContext(model),
                results,
                validateAllProperties: true);

            Assert.IsFalse(isValid);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(LoginViewModel.Username))));
        }

        [TestMethod]
        public void LoginViewModel_PasswordRequired_ValidationFails()
        {
            var model = new LoginViewModel
            {
                Username = "staff",
                Password = string.Empty
            };

            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(
                model,
                new ValidationContext(model),
                results,
                validateAllProperties: true);

            Assert.IsFalse(isValid);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(LoginViewModel.Password))));
        }

        [TestMethod]
        public void Login_Get_WhenUserIsAuthenticated_RedirectsToTicketsIndex()
        {
            var controller = new AccountController();

            var claims = new[] { new Claim(ClaimTypes.Name, "staff") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext();
            httpContext.User = user;

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

            var result = controller.Login();

            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));

            var redirect = (RedirectToActionResult)result;

            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Tickets", redirect.ControllerName);
        }

        [TestMethod]
        public async Task Login_Post_InvalidCredentials_ReturnsViewWithModelError()
        {
            var controller = new AccountController();

            // Ensure controller has a HttpContext so ModelState and other features work normally.
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            };

            var model = new LoginViewModel
            {
                Username = "wrong",
                Password = "creds"
            };

            var result = await controller.Login(model);

            // Should return the login view with the same model.
            Assert.IsInstanceOfType(result, typeof(ViewResult));

            var view = (ViewResult)result;
            Assert.AreSame(model, view.Model);

            // Controller should have added a model-level error for invalid credentials.
            Assert.IsFalse(controller.ModelState.IsValid);
            Assert.IsTrue(controller.ModelState.ContainsKey(string.Empty));
            var errors = controller.ModelState[string.Empty].Errors;
            Assert.IsTrue(errors.Any(e => e.ErrorMessage.Contains("Invalid username or password")));
        }
    }
}
