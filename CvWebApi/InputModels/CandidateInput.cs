using System.ComponentModel.DataAnnotations;

namespace CvWebApi.InputModels
{
    public class CandidateInput
    {
        [Required]
        public string FullName { get; set; } = null!;

        [Required, EmailAddress]
        public string EmailAddress { get; set; } = null!;

        public string? PhoneNumber { get; set; }

        public string? Location { get; set; }

        public string? Title { get; set; }

        public string? Summary { get; set; }

        // Base64-encoded profile picture (optional)
        public string? ProfilePic { get; set; }
    }
}
