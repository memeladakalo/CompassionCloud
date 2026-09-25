using System.ComponentModel.DataAnnotations;

namespace CompassionCloud.Models
{
    public class Volunteer
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        public string Skills { get; set; }

        [Required]
        [Display(Name = "Availability")]
        public string Availability { get; set; }

        public DateTime RegisteredDate { get; set; } = DateTime.Now;

        public string? UserId { get; set; }
    }
}