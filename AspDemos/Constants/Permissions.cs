using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace AspDemos.Constants {
    public class PermissionGroup {
        public string Module { get; set; }
        public List<PermissionAction> Actions { get; set; }
    }

    public class PermissionAction {
        public string Action { get; set; }
        public bool Selected { get; set; } = false;
    }

    internal class PermissionRequirement : IAuthorizationRequirement {
        public string Permission { get; private set; }
        public PermissionRequirement(string permission) {
            Permission = permission;
        }
    }

    public static class Permissions {

        public static List<string> actions = new List<string>() {
            "Create", "View", "Edit", "Delete"
        };

        public static List<string> modules = new List<string>() {
            "Employees", "Jobs"
        };

        public static List<string> GeneratePermissionsForModule(string module) {
            List<string> permissions = new List<string>();
            foreach (var action in actions) {
                permissions.Add($"Permissions.{module}.{action}");
            }

            return permissions;
        }

        public static List<string> GeneratePermissionsForAllModules() {
            List<string> permissions = new List<string>();
            foreach (var module in modules) {
                foreach (var action in actions) {
                    permissions.Add($"Permissions.{module}.{action}");
                }
            }

            return permissions;
        }
    }
}

