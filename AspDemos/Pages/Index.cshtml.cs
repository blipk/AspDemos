using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using AspDemos.Models.Identity;

namespace AspDemos.Pages {
    [Authorize]
    public class IndexModel : PageModel {
        private readonly ILogger<IndexModel> _logger;

        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(SignInManager<ApplicationUser> signInManager, ILogger<IndexModel> logger) {
            _signInManager = signInManager;
            _logger = logger;
        }

        public ActionResult OnGet() {
            //TODO Check roles or user preferences for correct redirect
            string returnUrl = "~/Admin";
            if (_signInManager.IsSignedIn(User)) {
                return Redirect(returnUrl);
            }

            return Page();
        }
    }
}