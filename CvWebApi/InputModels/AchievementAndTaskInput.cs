using System.ComponentModel.DataAnnotations;

namespace CvWebApi.InputModels
{
    public class AchievementAndTaskInput
    {
        [Required]
        public string Text { get; set; } = null!;

        [Required, EmailAddress]
        public string CandidateEmail { get; set; } = null!;

        [Required]
        public string EmployerName { get; set; } = null!;
    }
}
