using System;
using System.ComponentModel.DataAnnotations;

namespace CvWebApi.Models
{
    public class Picture
    {
        [Key]
        public Guid Id { get; set; }

        // Base64-encoded photo
        [Required]
        public string Photo { get; set; } = null!;
    }
}
