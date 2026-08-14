using System;
using System.ComponentModel.DataAnnotations;

namespace CvWebApi.InputModels
{
    public class WorkExperienceInput
    {
        [Required]
        public string EmployerName { get; set; } = null!;

        [Required]
        public string JobTitle { get; set; } = null!;

        public DateOnly StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public string? Location { get; set; }

        public string? Summary { get; set; }

        [Required, EmailAddress]
        public string CandidateEmail { get; set; } = null!;
    }
}
