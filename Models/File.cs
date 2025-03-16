using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveFlow_CRM_API.Models
{
    public class File
    {
        [Key]
        public int FileId { get; set; }

        // Required date fields
        [Required]
        public DateTime ScholarshipStartDate { get; set; }

        [Required]
        public DateTime CriminalRecordExpirDate { get; set; }

        [Required]
        public DateTime MedicalRecordExpirDate { get; set; }

        // Non-nullable PaymentId => required in DB. 
        // Navigation can still be null in C# if not loaded.
        [Required]
        public int PaymentId { get; set; }
        [ForeignKey(nameof(PaymentId))]
        public virtual Payment? Payment { get; set; }

        // Non-nullable StudentId => required in DB
        [Required]
        public string StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual ApplicationUser? Student { get; set; }

        // Non-nullable InstructorId => required in DB
        [Required]
        public string InstructorId { get; set; }
        [ForeignKey(nameof(InstructorId))]
        public virtual ApplicationUser? Instructor { get; set; }

        // Non-nullable VehicleId => required in DB
        [Required]
        public int VehicleId { get; set; }
        [ForeignKey(nameof(VehicleId))]
        public virtual Vehicle? Vehicle { get; set; }

        [Required]
        public int TeachingCategoryId { get; set; }

        [ForeignKey(nameof(TeachingCategoryId))]
        public virtual TeachingCategory? TeachingCategory { get; set; }

        
        public virtual ICollection<Appointment>? Appointments { get; set; }
    }
}
