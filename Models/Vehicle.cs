using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveFlow_CRM_API.Models
{
    public class Vehicle
    {
        [Key]
        public int VehicleId { get; set; }

        [Required]
        public string RegistrationNr { get; set; } = null!;

        // E.g. "M" or "A" for Manual/Automatic
        [Required]
        public string GearboxType { get; set; } = null!;

        [Required]
        public string Colour { get; set; } = null!;

        [Required]
        public DateTime ITP_ExpirDate { get; set; }

        [Required]
        public DateTime InsuranceExpirDate { get; set; }

        [Required]
        public DateTime RCA_ExpirDate { get; set; }

        // Foreign key to AutoSchool (1-to-many)
        [Required]
        public int AutoSchoolId { get; set; }
        [ForeignKey(nameof(AutoSchoolId))]
        public virtual AutoSchool? AutoSchool { get; set; }


        [Required]
        public int TeachingCategoryId { get; set; }

        [ForeignKey(nameof(TeachingCategoryId))]
        public virtual TeachingCategory? TeachingCategory { get; set; }

        public virtual ICollection<File>? Files { get; set; }
    }
}
