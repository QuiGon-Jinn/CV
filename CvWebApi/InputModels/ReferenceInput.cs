using System.ComponentModel.DataAnnotations;

namespace CvWebApi.InputModels
{
    public class ReferenceInput
    {
        [Required, EmailAddress]
        public string CandidateEmail { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;

        public string? Relationship { get; set; }

        [EmailAddress]
        public string? EmailAddress { get; set; }

        public string? PhoneNumber { get; set; }

        // Optional: link the reference to a specific work experience by employer name
        public string? EmployerName { get; set; }
    }
}
