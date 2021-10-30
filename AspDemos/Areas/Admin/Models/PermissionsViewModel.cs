using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using AspDemos.Constants;

namespace AspDemos.Areas.Admin.Models {
    public class PermissionViewModel {
        public string RoleId { get; set; }
        public string RoleName { get; set; }

        public List<PermissionGroup> RolePermissionGroups { get; set; }
    }
    /*
    public class PermissionClaimsViewModel {

        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
        
    public bool Selected { get; set; }
    }
    */
}
