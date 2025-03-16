using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveFlow_CRM_API.Models
{
    public class Request
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string PhoneNr { get; set; } = null!;

        [StringLength(50)]
        public string? DrivingCategory { get; set; }

        public DateTime RequestDate { get; set; }

        // Foreign key to AutoSchool
        public int AutoSchoolId { get; set; }

        [ForeignKey(nameof(AutoSchoolId))]
        public virtual AutoSchool? AutoSchool { get; set; }
    }
}
