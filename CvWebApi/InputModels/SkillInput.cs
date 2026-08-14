using System.ComponentModel.DataAnnotations;
using CvWebApi.Models;

namespace CvWebApi.InputModels
{
    public class SkillInput
    {
        [Required, EmailAddress]
        public string CandidateEmail { get; set; } = null!;

        // Optional: associate the skill with a specific work experience by employer name
        public string? EmployerName { get; set; }

        [Required]
        public string SkillName { get; set; } = null!;

        [Required]
        public SkillType SkillType { get; set; }

        [Required]
        public SkillLevel SkillLevel { get; set; }
    }
}
