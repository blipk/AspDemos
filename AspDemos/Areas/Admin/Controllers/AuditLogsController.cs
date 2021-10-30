using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspDemos.Models;
using Microsoft.AspNetCore.Identity;
using AspDemos.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using AspDemos.Data;
using AspDemos.Models.Identity;

namespace AspDemos.Areas.Admin.Controllers {
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")]
    public class AuditLogsController : Controller {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        public AuditLogsController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context) {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index() {
            var model = _context.AuditLogs.OrderByDescending(p => p.Id).Take(25).ToList();
            return View(model);
        }
    }
}
