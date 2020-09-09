using System.ComponentModel.DataAnnotations;

namespace NeplusLicense.Models
{
    public class PasswordChangeDto
    {
        [Required( ErrorMessage = "current password is required" )]
        public string CurrentPassword { get; set; }

        [Required( ErrorMessage = "new password is required" )]
        public string NewPassword { get; set; }
    }
}