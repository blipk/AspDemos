using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using AspDemos.Data;

namespace AspDemos.Areas.Admin.Controllers {
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")]
    public class RolesController : Controller {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        public RolesController(ApplicationDbContext context, RoleManager<IdentityRole> roleManager) {
            _roleManager = roleManager;
            _context = context;
        }
        public async Task<IActionResult> Index() {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string roleName) {
            if (roleName != null) {
                await _roleManager.CreateAsync(new IdentityRole(roleName.Trim()));
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string? id) {
            if (id == null) {
                return NotFound();
            }
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) {
                return NotFound();
            }
            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string? id, string name) {
            if (id == null) {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) {
                return NotFound();
            }

            if (ModelState.IsValid) {
                role.Name = name;
                var result = await _roleManager.UpdateAsync(role);
                return RedirectToAction(nameof(Index));
            }
            return View(role);
        }

        public async Task<IActionResult> Delete(string? id) {
            if (id == null) {
                return NotFound();
            }
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) {
                return NotFound();
            }
            return View(role);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string? id) {
            if (id == null) {
                return NotFound();
            }
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) {
                return NotFound();
            }

            var result = await _roleManager.DeleteAsync(role);
            return RedirectToAction(nameof(Index));
        }
    }
}
