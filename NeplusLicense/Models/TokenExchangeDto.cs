using System.ComponentModel.DataAnnotations;

namespace NeplusLicense.Models
{
    public class TokenExchangeDto
    {
        /// <summary>
        /// Short live access token.
        /// </summary>
        [Required( ErrorMessage = "Access Token is required" )]
        public string AccessToken { get; set; }

        /// <summary>
        /// Long live refresh token 
        /// </summary>
        [Required( ErrorMessage = "Refresh Token is required" )]
        public string RefreshToken { get; set; }

        /// <summary>
        /// When an user tries to logout on all connected devices, we need to confirm that the request coming from a valid user.
        /// </summary>
        public string CurrentPassword { get; set; }

        /// <summary>
        /// Whether the connected devices should logout.
        /// </summary>
        public bool? IsLogoutAll { get; set; }
    }
}