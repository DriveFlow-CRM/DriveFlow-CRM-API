// Payment.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveFlow_CRM_API.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        // Example fields
        public bool ScholarshipPayment { get; set; }
        public int SessionsPayed { get; set; }
    }
}
