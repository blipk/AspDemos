using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AspDemos.Constants;
using AspDemos.Helpers;
using AspDemos.Models.Identity;

namespace AspDemos.Data.Seeds {
    public static class ContextSeed {
        public static async Task SeedRolesAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) {
            // Seed Roles
            foreach (var role in Enum.GetValues(typeof(Enums.Roles))) {
                await roleManager.CreateAsync(new IdentityRole(role.ToString()));
            }
        }

        public static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) {
            // Seed default super admin user
            var defaultUser = new ApplicationUser {
                UserName = "superadmin",
                Email = "superadmin@isd.com",
                FirstName = "Super",
                LastName = "Admin",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };
            if (userManager.Users.All(u => u.Id != defaultUser.Id)) {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null) {
                    await userManager.CreateAsync(defaultUser, "123Pa$$word");
                    foreach (var role in Enum.GetValues(typeof(Enums.Roles))) {
                        await userManager.AddToRoleAsync(defaultUser, role.ToString());
                    }
                }
                await roleManager.SeedClaimsForSuperAdmin();
            }
        }

        private async static Task SeedClaimsForSuperAdmin(this RoleManager<IdentityRole> roleManager) {
            var adminRole = await roleManager.FindByNameAsync("SuperAdmin");
            await roleManager.AddPermissionClaimForAllModules(adminRole);
        }

    }
}
