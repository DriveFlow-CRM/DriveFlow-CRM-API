using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveFlow_CRM_API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // FK to AutoSchool (one-to-many)
        public int? AutoSchoolId { get; set; }
        [ForeignKey(nameof(AutoSchoolId))]
        public virtual AutoSchool? AutoSchool { get; set; }

        // One-to-many with InstructorAvailability
        public virtual ICollection<InstructorAvailability>? InstructorAvailabilities { get; set; }

        // Many-to-many bridging with TeachingCategory
        public virtual ICollection<ApplicationUserTeachingCategory>? ApplicationUserTeachingCategories { get; set; }
        // E.g. for “Student files”
        public virtual ICollection<File>? StudentFiles { get; set; }

        // E.g. for “Instructor files”
        public virtual ICollection<File>? InstructorFiles { get; set; }
    }

}
