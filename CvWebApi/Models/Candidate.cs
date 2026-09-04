using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CvWebApi.Models
{
    public class Candidate
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string FullName { get; set; } = null!;

        [Required, EmailAddress]
        public string EmailAddress { get; set; } = null!;

        public string? PhoneNumber { get; set; }

        public string? Location { get; set; }

        public string? Title { get; set; }

        public string? Summary { get; set; }

        public Guid? ProfilePicId { get; set; }

        [ForeignKey("ProfilePicId")]
        public Picture? ProfilePic { get; set; }

        // Password hash for authentication (not returned in API responses)
        [System.Text.Json.Serialization.JsonIgnore]
        public string? PasswordHash { get; set; }


        // Navigation collections
        public List<WorkExperience> WorkExperience { get; set; } = new();

        public List<Education> Education { get; set; } = new();

        public List<SoftSkill> SoftSkills { get; set; } = new();

        public List<Skill> Skills { get; set; } = new();

        public List<Interest> Interests { get; set; } = new();

        public List<Reference> References { get; set; } = new();
    }
}
