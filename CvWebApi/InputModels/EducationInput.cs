using System.ComponentModel.DataAnnotations;

namespace CvWebApi.InputModels
{
    public class EducationInput
    {
        [Required, EmailAddress]
        public string CandidateEmail { get; set; } = null!;

        [Required]
        public string InstituteName { get; set; } = null!;

        [Required]
        public string Qualification { get; set; } = null!;

        public DateOnly StartDate { get; set; }

        public DateOnly? EndDate { get; set; }
    }
}
