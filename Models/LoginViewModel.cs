using System.ComponentModel.DataAnnotations;

namespace NUTRIBITE.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        // kept for future extension (not used by controller)
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}