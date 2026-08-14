using System.ComponentModel.DataAnnotations;

namespace CvWebApi.InputModels
{
    public class InterestInput
    {
        [Required, EmailAddress]
        public string CandidateEmail { get; set; } = null!;

        public string? InterestName { get; set; }

        // Base64-encoded picture
        public string? InterestPicture { get; set; }
    }
}
