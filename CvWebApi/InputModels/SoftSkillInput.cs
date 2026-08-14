using System.ComponentModel.DataAnnotations;

namespace CvWebApi.InputModels
{
    public class SoftSkillInput
    {
        [Required, EmailAddress]
        public string CandidateEmail { get; set; } = null!;

        // Optional: textual name of the soft skill
        public string? SkillName { get; set; }

        // Optional: base64-encoded picture for the skill
        public string? SkillPicture { get; set; }
    }
}
