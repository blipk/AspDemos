using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AspDemos.Infrastructure.SoftDelete;
using AspDemos.Models.Shared;

namespace AspDemos.Models.Identity {
    public class ApplicationUser : IdentityUser, ICascadeSoftDelete {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public byte[]? ProfilePicture { get; set; }

        [DataType(DataType.Date)]
        [Column("DOB", TypeName = "datetime")]
        public DateTime? Dob { get; set; }

        public byte SoftDeleteLevel { get; set; }

        /*
        public string? Gender { get; set; }
        */
    }

}
