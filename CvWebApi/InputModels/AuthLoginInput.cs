using System.ComponentModel.DataAnnotations;

namespace CvWebApi.InputModels
{
    public class AuthLoginInput
    {
        [Required, EmailAddress]
        public string EmailAddress { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}
