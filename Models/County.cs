using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveFlow_CRM_API.Models
{
    public class County
    {
        [Key]
        public int CountyId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(10)]
        public string? Abbreviation { get; set; }

        // 1–n relationship with City
        public virtual ICollection<City>? Cities { get; set; }
    }
}
