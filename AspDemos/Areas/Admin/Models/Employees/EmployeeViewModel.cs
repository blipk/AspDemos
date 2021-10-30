using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using AspDemos.Areas.Admin.Models.Identity;
using AspDemos.Models;

namespace AspDemos.Areas.Admin.Models.Employees {
    public class EmployeeViewModel {
        public Employee Employee { get; set; }
        public List<ManageUserRolesViewModel> Roles { get; set; } = new List<ManageUserRolesViewModel>();
    }
}