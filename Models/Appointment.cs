using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveFlow_CRM_API.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan StartHour { get; set; }

        [Required]
        public TimeSpan EndHour { get; set; }

        // Foreign key to File (required in DB)
        [Required]
        public int FileId { get; set; }

        // Navigation property
        [ForeignKey(nameof(FileId))]
        public virtual File? File { get; set; }

        [ForeignKey(nameof(User))]
        [Required]
        public string UserId { get; set; }

        public virtual ApplicationUser? User { get; set; }

    }
}
