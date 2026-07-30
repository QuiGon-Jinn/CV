using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvWebApi.Models
{
    public class Interest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CandidateId { get; set; }

        [ForeignKey("CandidateId")]
        public Candidate? Candidate { get; set; }

        public string? InterestName { get; set; }

        public Guid? InterestPictureId { get; set; }

        [ForeignKey("InterestPictureId")]
        public Picture? InterestPicture { get; set; }
    }
}
