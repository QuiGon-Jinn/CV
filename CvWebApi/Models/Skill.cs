using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvWebApi.Models
{
    public class Skill
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CandidateId { get; set; }

        [ForeignKey("CandidateId")]
        public Candidate? Candidate { get; set; }

        public Guid? WorkExperienceId { get; set; }

        [ForeignKey("WorkExperienceId")]
        public WorkExperience? WorkExperience { get; set; }

        [Required]
        public string SkillName { get; set; } = null!;

        [Required]
        public SkillLevel SkillLevel { get; set; }

        [Required]
        public SkillType SkillType { get; set; }
    }
}
