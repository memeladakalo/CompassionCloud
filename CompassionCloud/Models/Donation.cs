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
        [Range(typeof(decimal), "1", "10000000", ErrorMessage = "Amount must be between 1 and 10,000,000.")]
        public decimal Amount { get; set; }

        [Required]
        [RegularExpression("^(ZAR|USD|EUR)$", ErrorMessage = "Currency must be ZAR, USD or EUR.")]
        public string Currency { get; set; } // ZAR, USD, EUR

        [Required]
        [RegularExpression("^(One-time|Recurring)$", ErrorMessage = "Donation type must be One-time or Recurring.")]
        public string DonationType { get; set; } // One-time, Recurring

        public DateTime DonationDate { get; set; } = DateTime.Now;

        public string? UserId { get; set; } // For logged-in users

        public string ReferenceNumber { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

        public bool IsAnonymous { get; set; }
    }
}