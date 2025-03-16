using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveFlow_CRM_API.Models
{
    public class TeachingCategory
    {
        [Key]
        public int TeachingCategoryId { get; set; }

        [Required]
        public string CategoryName { get; set; } = null!;

        [Required]
        public decimal SessionCost { get; set; }

        [Required]
        public int SessionDuration { get; set; }

        [Required]
        public decimal ScholarshipPrice { get; set; }

        [Required]
        public int AutoSchoolId { get; set; }

        [ForeignKey(nameof(AutoSchoolId))]
        public virtual AutoSchool? AutoSchool { get; set; }

        public virtual ICollection<ApplicationUserTeachingCategory>? ApplicationUserTeachingCategories { get; set; }
        public virtual ICollection<File>? Files { get; set; }
        public virtual ICollection<Vehicle>? Vehicles { get; set; }

        // Added the Type field with validation for all standard driving license categories.
        [Required]
        [RegularExpression("^(AM|A1|A2|A|B1|B|BE|C1|C1E|C|CE|D1|D1E|D|DE)$")]
        public string Type { get; set; } = null!;

    }
}
