using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using AspDemos.Constants;
using System.Security.Claims;
using AspDemos.Areas.Admin.Models;
using System.Reflection;
using System.Web;

namespace AspDemos.Helpers {


    public static class PermissionsHelper {

        public static async Task RemovePermissionClaim(this RoleManager<IdentityRole> roleManager, IdentityRole role, string permission) {
            var allClaims = await roleManager.GetClaimsAsync(role);
            if (allClaims.Any(a => a.Type == "Permission" && a.Value == permission)) {
                await roleManager.RemoveClaimAsync(role, new Claim("Permission", permission));
            }
        }

        public static async Task AddPermissionClaim(this RoleManager<IdentityRole> roleManager, IdentityRole role, string permission) {
            var allClaims = await roleManager.GetClaimsAsync(role);
            if (!allClaims.Any(a => a.Type == "Permission" && a.Value == permission)) {
                await roleManager.AddClaimAsync(role, new Claim("Permission", permission));
            }
        }

        public static async Task AddPermissionClaimForModule(this RoleManager<IdentityRole> roleManager, IdentityRole role, string module) {
            //TODO check module string against constant, or another method
            var allClaims = await roleManager.GetClaimsAsync(role);
            var allPermissions = Permissions.GeneratePermissionsForModule(module);
            foreach (var permission in allPermissions) {
                if (!allClaims.Any(a => a.Type == "Permission" && a.Value == permission)) {
                    await roleManager.AddClaimAsync(role, new Claim("Permission", permission));
                }
            }
        }

        public static async Task AddPermissionClaimForAllModules(this RoleManager<IdentityRole> roleManager, IdentityRole role) {
            var allClaims = await roleManager.GetClaimsAsync(role);
            foreach (var module in Permissions.modules) {
                var allPermissions = Permissions.GeneratePermissionsForModule(module);
                foreach (var permission in allPermissions) {
                    if (!allClaims.Any(a => a.Type == "Permission" && a.Value == permission)) {
                        await roleManager.AddClaimAsync(role, new Claim("Permission", permission));
                    }
                }
            }
        }

        public async static Task<IList<Claim>> GetAllPermissionClaimsAsync(this RoleManager<IdentityRole> roleManager) {
            // SuperAdmin has been seeded with all the permissions and will be our permission reference
            var adminRole = await roleManager.FindByNameAsync("SuperAdmin");
            var allPermissionsClaims = await roleManager.GetClaimsAsync(adminRole);

            return allPermissionsClaims;
        }

        public static List<PermissionGroup> GetAllPermissionGroups() {
            List<PermissionGroup> permissionGroups = new List<PermissionGroup>();
            var actions = new List<PermissionAction>();
            var permissionGroup = new PermissionGroup();
            foreach (var module in Permissions.modules) {
                permissionGroup.Module = module;
                foreach (var action in Permissions.actions) {
                    actions.Add(new PermissionAction { Action = action });
                }
                permissionGroup.Actions = actions;
                permissionGroups.Add(permissionGroup);
            }
            return permissionGroups;
        }

        public async static Task<List<PermissionGroup>> GetAllPermissionClaimsGroupsAsync(this RoleManager<IdentityRole> roleManager) {
            var adminRole = await roleManager.FindByNameAsync("SuperAdmin");
            var allPermissionsClaims = await roleManager.GetClaimsAsync(adminRole);

            List<PermissionGroup> permissionGroups = allPermissionsClaims.GroupBy(
                s => s.Value.Split('.')[1],
                s => new PermissionAction { Action = s.Value.Split('.')[2]},
                (module, actions) => new PermissionGroup { Module = module, Actions = new List<PermissionAction>(actions.ToList())})
                .ToList();

            return permissionGroups;
        }

        public async static Task<List<PermissionGroup>> GetAllPermissionClaimsGroupsForRoleAsync(this RoleManager<IdentityRole> roleManager, IdentityRole role) {
            var allPermissionGroups = await GetAllPermissionClaimsGroupsAsync(roleManager);
            var allPermissionsClaims = await GetAllPermissionClaimsAsync(roleManager);
            var allClaimsForRole = await roleManager.GetClaimsAsync(role);
            var allClaimValues = allPermissionsClaims.Select(a => a.Value).ToList();
            var roleClaimValues = allClaimsForRole.Select(a => a.Value).ToList();
            var authorizedClaims = allClaimValues.Intersect(roleClaimValues).ToList();

            for (int i = 0; i < allPermissionGroups.Count(); i++) {
                for (int ii = 0; ii < allPermissionGroups[i].Actions.Count(); ii++) {
                    string claimValue = $"Permissions.{allPermissionGroups[i].Module}.{allPermissionGroups[i].Actions[ii].Action}";
                    if (authorizedClaims.Any(a => a == claimValue)) {
                        allPermissionGroups[i].Actions[ii].Selected = true;
                    }
                }

            }
            return allPermissionGroups;
        }
    }
}