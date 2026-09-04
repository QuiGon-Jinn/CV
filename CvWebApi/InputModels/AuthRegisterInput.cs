using System.ComponentModel.DataAnnotations;

namespace CvWebApi.InputModels
{
    public class AuthRegisterInput
    {
        [Required]
        public string FullName { get; set; } = null!;

        [Required, EmailAddress]
        public string EmailAddress { get; set; } = null!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = null!;
    }
}
