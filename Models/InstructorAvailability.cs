using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace DriveFlow_CRM_API.Models
{
    public class InstructorAvailability
    {
        [Key]
        public int InstructorAvailabilityId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan StartHour { get; set; }

        [Required]
        public TimeSpan EndHour { get; set; }


        [ForeignKey(nameof(ApplicationUserId))]
        [Required]
        public string ApplicationUserId { get; set; }

        public virtual ApplicationUser? ApplicationUser { get; set; }
    }
}
