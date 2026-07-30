using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CvWebApi.Models
{
    public class WorkExperience
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CandidateId { get; set; }

        [ForeignKey("CandidateId")]
        public Candidate? Candidate { get; set; }

        [Required]
        public string JobTitle { get; set; } = null!;

        [Required]
        public string EmployerName { get; set; } = null!;

        public DateOnly StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public string? Location { get; set; }

        public string? Summary { get; set; }

        // Skills related to this work experience
        public List<Skill> Skills { get; set; } = new();
        // Achievements and tasks related to this work experience
        public List<AchievementsAndTasks> AchievementsAndTasks { get; set; } = new();

        // References related to this work experience
        public List<Reference> References { get; set; } = new();
    }
}
