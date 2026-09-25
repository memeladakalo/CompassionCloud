using System.ComponentModel.DataAnnotations;

namespace CompassionCloud.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "I am registering as")]
        public string Role { get; set; } // "Donor", "Volunteer", or "Employee"

        [Display(Name = "I agree to the Terms & Conditions and Privacy Policy")]
        public bool AgreeToTerms { get; set; }
    }
}