using Microsoft.EntityFrameworkCore;
using CvWebApi.Models;

namespace CvWebApi.Data
{
    public class CvDbContext : DbContext
    {
        public CvDbContext(DbContextOptions<CvDbContext> options)
            : base(options)
        {
        }

        public DbSet<Candidate> Candidates { get; set; } = null!;
        public DbSet<Picture> Pictures { get; set; } = null!;
        public DbSet<WorkExperience> WorkExperiences { get; set; } = null!;
        public DbSet<AchievementsAndTasks> AchievementsAndTasks { get; set; } = null!;
        public DbSet<Skill> Skills { get; set; } = null!;
        public DbSet<Education> Educations { get; set; } = null!;
        public DbSet<Reference> References { get; set; } = null!;
        public DbSet<SoftSkill> SoftSkills { get; set; } = null!;
        public DbSet<Interest> Interests { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure any relationships or table mappings if needed in future
        }
    }
}
