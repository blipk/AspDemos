using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AspDemos.Models.Shared {
    [Owned]
    public class Address {
        [StringLength(255)]
        [Display(Name = "Address")]
        public string? NumberAndStreet { get; set; }
        [StringLength(255)]
        public string? Suburb { get; set; }
        [StringLength(255)]
        public string? City { get; set; }
        [StringLength(255)]
        public string? State { get; set; }
        [StringLength(255)]
        public string? PostCode { get; set; }
        [StringLength(255)]
        public string? CountryCodeIso2 { get; set; }

    }
}
