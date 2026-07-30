using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvWebApi.Models
{
    public class SoftSkill
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CandidateId { get; set; }

        [ForeignKey("CandidateId")]
        public Candidate? Candidate { get; set; }

        public string? SkillName { get; set; }

        public Guid? SkillPictureId { get; set; }

        [ForeignKey("SkillPictureId")]
        public Picture? SkillPicture { get; set; }
    }
}
