using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AspDemos.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using AspDemos.Data;
using SoftDeleteServices.Concrete;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AspDemos.Models.Identity;
using AspDemos.Infrastructure.SoftDelete;

namespace AspDemos.Areas.Admin.Controllers {
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")]
    public class UsersController : Controller {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        public UsersController(ApplicationDbContext context, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) {
            _context = context;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index() {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            var allUsersExceptCurrentUser = await _userManager.Users.Where(a => a.Id != currentUser.Id).ToListAsync();
            //var allUsersExceptCurrentUser = await _userManager.Users.ToListAsync();
            var users = await _userManager.Users.ToListAsync();
            var usersViewModelList = new List<UsersViewModel>();
            foreach (ApplicationUser user in allUsersExceptCurrentUser) {
                var usersViewModel = new UsersViewModel();
                usersViewModel.ApplicationUser = user;
                usersViewModel.Roles = await GetUserRoles(user);
                usersViewModelList.Add(usersViewModel);
            }
            return View(usersViewModelList);
        }
        private async Task<List<string>> GetUserRoles(ApplicationUser user) {
            return new List<string>(await _userManager.GetRolesAsync(user));
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoles(string userId) {
            ViewBag.userId = userId;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) {
                return NotFound();
            }
            ViewBag.UserName = user.UserName;
            var model = new List<ManageUserRolesViewModel>();
            foreach (var role in _roleManager.Roles) {
                var userRolesViewModel = new ManageUserRolesViewModel {
                    RoleId = role.Id,
                    RoleName = role.Name
                };
                if (await _userManager.IsInRoleAsync(user, role.Name)) {
                    userRolesViewModel.Selected = true;
                } else {
                    userRolesViewModel.Selected = false;
                }
                model.Add(userRolesViewModel);
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ManageRoles(List<ManageUserRolesViewModel> model, string userId) {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) {
                return NotFound();
            }
            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);
            if (!result.Succeeded) {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return View(model);
            }
            result = await _userManager.AddToRolesAsync(user, model.Where(x => x.Selected).Select(y => y.RoleName));
            if (!result.Succeeded) {
                ModelState.AddModelError("", "Cannot add selected roles to user");
                return View(model);
            }

            // Refresh sign ins
            await _signInManager.RefreshSignInAsync(user);

            // Refresh current user sign ins
            var currentUser = await _userManager.GetUserAsync(User);
            await _signInManager.RefreshSignInAsync(currentUser);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(string? id) {
            if (id == null) {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id) {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) {
                return NotFound();
            }

            var service = _context.GetService<CascadeSoftDelServiceAsync<ICascadeSoftDelete>>();
            var res = await service.SetCascadeSoftDeleteAsync(user);

            //await _userManager.DeleteAsync(user);

            return RedirectToAction(nameof(Index));
        }
    }
}
