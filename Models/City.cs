using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveFlow_CRM_API.Models
{
    public class City
    {
        [Key]
        public int CityId { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = null!;

        // Foreign key to County
        public int CountyId { get; set; }

        [ForeignKey(nameof(CountyId))]
        public virtual County? County { get; set; }

        // One-to-many relationship: a City has multiple Addresses
        public virtual ICollection<Address>? Addresses { get; set; }
    }
}
