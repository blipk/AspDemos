using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using AspDemos.Models.Identity;
using AspDemos.Areas.Admin.Models.Identity;

namespace AspDemos.Areas.Admin.Controllers {
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")]
    public class AccountsController : Controller {

        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<AccountsController> _logger;
        private readonly IEmailSender _emailSender;

        public AccountsController(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountsController> logger,
            IEmailSender emailSender) {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<IActionResult> Register() {
            var model = new List<RegisterViewModel>();

            return PartialView(model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model) {
            bool validateOnly = false;
            var formKeys = Request.Form.Keys;
            if (formKeys.Contains("validateOnly")) {
                validateOnly = true;
            }

            if (ModelState.IsValid) {
                // Skip database options if only using this request to validate the form data against the model - for multiform views
                var user = CreateUser();

                //FormCollection  RegisterViewModelform
                await _userStore.SetUserNameAsync(user, model.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, model.Email, CancellationToken.None);
                // Add custom fields
                user.UserName = model.Username;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Dob = Convert.ToDateTime(model.Dob);

                var checkUserName = await _userManager.FindByNameAsync(user.UserName);
                if (checkUserName != null) {
                    Response.StatusCode = 422;  
                    ModelState.AddModelError(string.Empty, "Username is already taken.");
                    return PartialView();
                }

                var result = await _userManager.CreateAsync(user, model.Password);

                foreach (var error in result.Errors) {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                if (validateOnly) {
                    await _userManager.DeleteAsync(user);
                    return PartialView();
                }

                if (result.Succeeded) {
                    _logger.LogInformation("User created a new account with password.");

                    await _userManager.AddToRoleAsync(user, Enums.Roles.StandardUser.ToString());
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code },
                        protocol: Request.Scheme);

 
                    await _emailSender.SendEmailAsync(model.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    // Send data back to be used by employee etc form
                    return Json(new { userId = userId, confirmationCode = code, confirmationLink = callbackUrl });
                    /*
                    if (_userManager.Options.SignIn.RequireConfirmedAccount) {
                        return RedirectToPage("RegisterConfirmation", new { email = model.Email });
                    } else {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                    }
                    */

                } else {
                    Response.StatusCode = 422;
                }
                
            } else { // Validation failed status code
                Response.StatusCode = 422;
            }
            
            // If we got this far, something failed, redisplay form
            return PartialView();
        }

        private ApplicationUser CreateUser() {
            try {
                return Activator.CreateInstance<ApplicationUser>();
            } catch {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }
        private IUserEmailStore<ApplicationUser> GetEmailStore() {
            if (!_userManager.SupportsUserEmail) {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
