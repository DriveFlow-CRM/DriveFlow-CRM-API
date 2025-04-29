// ApplicationUserTeachingCategories.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveFlow_CRM_API.Models
{
    public class ApplicationUserTeachingCategory
    {
        [Key]
        public int UserTeachingCategoryId { get; set; } // Independent PK

        // Foreign key to ApplicationUser
        [Required]
        public string UserId { get; set; } // This is not part of the PK

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        // Foreign key to TeachingCategory
        [Required]
        public int TeachingCategoryId { get; set; }

        [ForeignKey(nameof(TeachingCategoryId))]
        public virtual TeachingCategory? TeachingCategory { get; set; }

        // Additional columns if needed
        // public DateTime AssignedDate { get; set; }
    }
}
