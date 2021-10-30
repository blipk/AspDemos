using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using AspDemos.Data;
using AspDemos.Models.Shared;
using AspDemos.Models.Identity;

namespace AspDemos.Models {
    [Table("Employees")]
    [Index(nameof(EmployeeNumber))]
    public class Employee : BaseEntity {
        [ValidateNever]
        public string ApplicationUserId { get; set; }
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(255)]
        public string EmployeeNumber { get; set; } = null!;

        public Address Address { get; set; }

        [StringLength(100)]
        public string? FacebookProfile { get; set; }

        [StringLength(100)]
        public string? TwitterProfile { get; set; }

        [StringLength(100)]
        public string? InstagramProfile { get; set; }

        [StringLength(1000)]
        public string? LinkedInProfile { get; set; }

    }

    public class EmployeeEntityConfiguration : IEntityTypeConfiguration<Employee> {
        public void Configure(EntityTypeBuilder<Employee> builder) {
            
        }
    }
}