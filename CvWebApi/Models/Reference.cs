using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvWebApi.Models
{
    public class Reference
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string? Relationship { get; set; }

        [EmailAddress]
        public string? EmailAddress { get; set; }

        public string? PhoneNumber { get; set; }

        [Required]
        public Guid CandidateId { get; set; }

        [ForeignKey("CandidateId")]
        public Candidate? Candidate { get; set; }

        public Guid? WorkExperienceId { get; set; }

        [ForeignKey("WorkExperienceId")]
        public WorkExperience? WorkExperience { get; set; }
    }
}
