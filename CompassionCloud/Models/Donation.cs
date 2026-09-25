using System;
using System.ComponentModel.DataAnnotations;

namespace CompassionCloud.Models
{
    public class Donation
    {
        public int Id { get; set; }

        [Required]
        public string DonorName { get; set; }

        [Required]
        [EmailAddress]
        public string DonorEmail { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string Currency { get; set; } // ZAR, USD, EUR

        [Required]
        public string DonationType { get; set; } // One-time, Recurring

        public DateTime DonationDate { get; set; } = DateTime.Now;

        public string? UserId { get; set; } // For logged-in users

        public string ReferenceNumber { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

        public bool IsAnonymous { get; set; }
    }
}