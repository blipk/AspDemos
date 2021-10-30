using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using AspDemos.Infrastructure.SoftDelete;

namespace AspDemos.Models.Shared {
    public abstract class BaseEntity : ICascadeSoftDelete {
        [Key]
        [ScaffoldColumn(false)]
        public Guid Id { get; set; }

        [ScaffoldColumn(false)]
        public DateTime CreatedDate { get; set; }

        [MaxLength(256)]
        [ScaffoldColumn(false)]
        public string? CreatedBy { get; set; }

        [ScaffoldColumn(false)]
        public DateTime UpdatedDate { get; set; }

        [MaxLength(256)]
        [ScaffoldColumn(false)]
        public string? UpdatedBy { get; set; }

        [ScaffoldColumn(false)]
        public byte SoftDeleteLevel { get; set; }
    }




}
