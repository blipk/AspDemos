using Microsoft.AspNetCore.Mvc;
using AspDemos.Areas.Admin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using AspDemos.Constants;
using AspDemos.Helpers;
using AspDemos.Data;

namespace AspDemos.Areas.Admin.Controllers {
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")]
    public class PermissionsController : Controller {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        public PermissionsController(ApplicationDbContext context, RoleManager<IdentityRole> roleManager) {
            _context = context;
            _roleManager = roleManager;
        }

        public async Task<ActionResult> Index(string roleId) {
            var model = new PermissionViewModel();
            var adminRole = await _roleManager.FindByNameAsync("SuperAdmin");
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == adminRole) {
                return RedirectToAction("Index", "Roles");
            }

            model.RoleId = roleId;
            model.RoleName = role.Name;

            var allPermissionClaimsGroupsForRole = await PermissionsHelper.GetAllPermissionClaimsGroupsForRoleAsync(_roleManager, role);
            model.RolePermissionGroups = allPermissionClaimsGroupsForRole;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Update(PermissionViewModel model) {
            var adminRole = await _roleManager.FindByNameAsync("SuperAdmin");
            var role = await _roleManager.FindByIdAsync(model.RoleId);

            if (role == adminRole) {
                return RedirectToAction("Index", "Roles");
            }

            var claims = await _roleManager.GetClaimsAsync(role);

            foreach (var permissionGroup in model.RolePermissionGroups) {
                var removedClaims = permissionGroup.Actions.Where(a => !a.Selected).ToList();
                foreach (var claim in removedClaims) {
                    var claimValue = $"Permissions.{permissionGroup.Module}.{claim.Action}";
                    await PermissionsHelper.RemovePermissionClaim(_roleManager, role, claimValue);
                }

                var selectedClaims = permissionGroup.Actions.Where(a => a.Selected).ToList();
                foreach (var claim in selectedClaims) {
                    var claimValue = $"Permissions.{permissionGroup.Module}.{claim.Action}";
                    await PermissionsHelper.AddPermissionClaim(_roleManager, role, claimValue);
                }
            }

            //return RedirectToAction("Index", new { roleId = model.RoleId });
            return RedirectToAction("Index", "Roles");
        }
    }

}
