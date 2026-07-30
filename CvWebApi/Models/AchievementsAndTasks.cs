using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvWebApi.Models
{
    public class AchievementsAndTasks
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid WorkExperienceId { get; set; }

        [ForeignKey("WorkExperienceId")]
        public WorkExperience? WorkExperience { get; set; }

        [Required]
        public string Text { get; set; } = null!;
    }
}
