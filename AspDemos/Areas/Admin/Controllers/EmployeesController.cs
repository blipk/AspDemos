using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AspDemos.Areas.Admin.Models.Employees;
using AspDemos.Data;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using AspDemos.Areas.Admin.Models;
using AspDemos.Models;
using SoftDeleteServices.Concrete;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AspDemos.Models.Identity;
using AspDemos.Infrastructure.SoftDelete;

namespace AspDemos.Areas.Admin.Controllers {
    [Area("Admin")]
    [Authorize]
    public class EmployeesController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _userId;

        public EmployeesController(ApplicationDbContext context, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor) {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _userId = httpContextAccessor.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier).Value;
        }

        // GET: Admin/Employees
        [Authorize("Permissions.Employees.View")]
        public async Task<IActionResult> Index() {
            return View(await _context.Employee.ToListAsync());
        }

        // GET: Admin/Employees/Details/5
        [Authorize("Permissions.Employees.View")]
        public async Task<IActionResult> Details(Guid? id) {
            if (id == null) {
                return NotFound();
            }

            var employee = await _context.Employee
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null) {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Admin/Employees/Create
        [Authorize("Permissions.Employees.Create")]
        public async Task<IActionResult> Create() {
            var model = new EmployeeViewModel();

            foreach (var role in _roleManager.Roles) {
                if (role.Name.Contains("Employee")) {
                    var userRolesViewModel = new ManageUserRolesViewModel {
                        RoleId = role.Id,
                        RoleName = role.Name,
                        Selected = false
                    };

                    model.Roles.Add(userRolesViewModel);
                }
            }
            return View(model);
        }

        // POST: Admin/Employees/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize("Permissions.Employees.Create")]
        public async Task<IActionResult> Create(EmployeeViewModel employeeCreateViewModel) {
            bool validateOnly = false;
            var formKeys = Request.Form.Keys;

            var roles = employeeCreateViewModel.Roles;
            

            //employeeCreateViewModel.Employee.ApplicationUserId = Request.Form["userId"];
            if (formKeys.Contains("validateOnly")) {
                validateOnly = true;
                //employeeCreateViewModel.Employee.ApplicationUserId = "74a91875-9b3e-4d43-976e-4da48842714a";
            }


            //ModelState.ClearValidationState(nameof(employeeCreateViewModel));
            ModelState.Clear();
            await TryUpdateModelAsync(employeeCreateViewModel);

            if (ModelState.IsValid) {
                if (validateOnly) {
                    return View(employeeCreateViewModel);
                }

                var userId = Request.Form["userId"];
                var user = await _userManager.FindByIdAsync(userId);
                var applicationUser = await _userManager.FindByIdAsync(userId);
                employeeCreateViewModel.Employee.ApplicationUser = applicationUser;

                // Create employee
                _context.Add(employeeCreateViewModel.Employee);
                await _context.SaveChangesAsync();

                // Update roles
                await _userManager.AddToRoleAsync(user, "FieldStaff");
                foreach (var role in roles) {
                    if (role.Selected == true) {
                        await _userManager.AddToRoleAsync(user, role.RoleName);
                    }
                }

                return RedirectToAction(nameof(Index));
            } else {
                // Failure
                Response.StatusCode = 422;
            }


            return View(employeeCreateViewModel);
        }

        // GET: Admin/Employees/Edit/5
        public async Task<IActionResult> Edit(Guid? id) {
            var model = new EmployeeViewModel();
            if (id == null) {
                return NotFound();
            }

            var employee = await _context.Employee.Include(x => x.ApplicationUser).SingleOrDefaultAsync(x => x.Id == id);
            if (employee == null) {
                return NotFound();
            }

            var applicationUser = await _userManager.Users.SingleOrDefaultAsync(u => u.Id == employee.ApplicationUser.Id);
            employee.ApplicationUser = applicationUser;
            model.Employee = employee;

            return View(model);
        }

        // POST: Admin/Employees/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EmployeeViewModel model) {
            var employee = model.Employee;
            if (id != employee.Id) {
                return NotFound();
            }

            var applicationUser = await _userManager.Users.SingleOrDefaultAsync(u => u.Id == employee.ApplicationUser.Id);
            employee.ApplicationUser = applicationUser;

            if (ModelState.IsValid) {
                try {
                    //_context.Update(employee);
                    // Update only changed values
                    var oldEmployee = await _context.Employee.Include(x => x.ApplicationUser).SingleOrDefaultAsync(x => x.Id == id);
                    var empEntity = _context.Entry(oldEmployee);
                    empEntity.CurrentValues.SetValues(employee);
                    // Owned entities aren't updated with SetValues
                    var addressEntity = empEntity.Reference("Address");
                    addressEntity.TargetEntry.CurrentValues.SetValues(employee.Address);

                    await _context.SaveChangesAsync();
                } catch (DbUpdateConcurrencyException) {
                    if (!EmployeeExists(employee.Id)) {
                        return NotFound();
                    } else {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            } else {
                Response.StatusCode = 422;
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(Guid? id) {
            if (id == null) {
                return NotFound();
            }

            var employee = await _context.Employee
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null) {
                return NotFound();
            }

            return View(employee);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id) {
            var employee = await _context.Employee.Include(x => x.ApplicationUser).SingleOrDefaultAsync(x => x.Id == id);

            if (employee == null) {
                return NotFound();
            }


            var service = _context.GetService<CascadeSoftDelServiceAsync<ICascadeSoftDelete>>();
            var res = await service.SetCascadeSoftDeleteAsync(employee.ApplicationUser);

            //_context.Employee.Remove(employee);
            //await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(Guid id) {
            return _context.Employee.Any(e => e.Id == id);
        }
    }
}
