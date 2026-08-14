using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CvWebApi.Data;
using CvWebApi.InputModels;
using CvWebApi.Models;

namespace CvWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CvController : ControllerBase
    {
        private readonly CvDbContext _db;

        public CvController(CvDbContext db)
        {
            _db = db;
        }

        [HttpPost("AddCv")]
        public async Task<IActionResult> AddCv([FromBody] CandidateInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Try to find existing candidate by email (case-insensitive)
            var normalizedEmail = input.EmailAddress.Trim().ToLowerInvariant();
            var existing = await _db.Candidates
                .FirstOrDefaultAsync(c => c.EmailAddress.ToLower() == normalizedEmail);

            if (existing != null)
            {
                // Update fields
                existing.FullName = input.FullName;
                existing.PhoneNumber = input.PhoneNumber;
                existing.Location = input.Location;
                existing.Title = input.Title;
                existing.Summary = input.Summary;

                if (!string.IsNullOrWhiteSpace(input.ProfilePic))
                {
                    var pic = new Picture { Id = Guid.NewGuid(), Photo = input.ProfilePic };
                    _db.Pictures.Add(pic);
                    existing.ProfilePicId = pic.Id;
                }

                await _db.SaveChangesAsync();
                return Ok(existing);
            }

            // Insert new candidate
            var candidate = new Candidate
            {
                Id = Guid.NewGuid(),
                FullName = input.FullName,
                EmailAddress = input.EmailAddress,
                PhoneNumber = input.PhoneNumber,
                Location = input.Location,
                Title = input.Title,
                Summary = input.Summary
            };

            if (!string.IsNullOrWhiteSpace(input.ProfilePic))
            {
                var pic = new Picture { Id = Guid.NewGuid(), Photo = input.ProfilePic };
                _db.Pictures.Add(pic);
                candidate.ProfilePicId = pic.Id;
            }

            _db.Candidates.Add(candidate);
            await _db.SaveChangesAsync();

            return Ok(candidate);
        }

        [HttpPost("AddWorkExperience")]
        public async Task<IActionResult> AddWorkExperience([FromBody] WorkExperienceInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var normalizedCandidateEmail = input.CandidateEmail.Trim().ToLowerInvariant();
            var candidate = await _db.Candidates
                .FirstOrDefaultAsync(c => c.EmailAddress.ToLower() == normalizedCandidateEmail);

            if (candidate == null)
                return NotFound(new { Message = "Candidate not found for provided email." });

            var work = new WorkExperience
            {
                Id = Guid.NewGuid(),
                CandidateId = candidate.Id,
                EmployerName = input.EmployerName,
                JobTitle = input.JobTitle,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                Location = input.Location,
                Summary = input.Summary
            };

            _db.WorkExperiences.Add(work);
            await _db.SaveChangesAsync();

            return Ok(work);
        }

        [HttpPost("AddAchievementAndTask")]
        public async Task<IActionResult> AddAchievementAndTask([FromBody] AchievementAndTaskInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var normalizedCandidateEmail = input.CandidateEmail.Trim().ToLowerInvariant();
            var candidate = await _db.Candidates
                .FirstOrDefaultAsync(c => c.EmailAddress.ToLower() == normalizedCandidateEmail);

            if (candidate == null)
                return NotFound(new { Message = "Candidate not found for provided email." });

            // Find a work experience record for this candidate and employer name (case-insensitive)
            var normalizedEmployer = input.EmployerName.Trim().ToLowerInvariant();
            var work = await _db.WorkExperiences
                .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null && w.EmployerName.ToLower() == normalizedEmployer);

            // If exact case-insensitive match not found, try partial match (contains)
            if (work == null)
            {
                work = await _db.WorkExperiences
                    .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null && w.EmployerName.ToLower().Contains(normalizedEmployer));
            }

            if (work == null)
                return NotFound(new { Message = "Work experience not found for provided employer and candidate." });

            var achievement = new AchievementsAndTasks
            {
                Id = Guid.NewGuid(),
                WorkExperienceId = work.Id,
                Text = input.Text
            };

            _db.AchievementsAndTasks.Add(achievement);
            await _db.SaveChangesAsync();

            return Ok(achievement);
        }

        [HttpPost("AddSkill")]
        public async Task<IActionResult> AddSkill([FromBody] SkillInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var normalizedCandidateEmail = input.CandidateEmail.Trim().ToLowerInvariant();
            var candidate = await _db.Candidates
                .FirstOrDefaultAsync(c => c.EmailAddress.ToLower() == normalizedCandidateEmail);

            if (candidate == null)
                return NotFound(new { Message = "Candidate not found for provided email." });

            Guid? workId = null;
            if (!string.IsNullOrWhiteSpace(input.EmployerName))
            {
                var normalizedEmployer = input.EmployerName.Trim().ToLowerInvariant();
                var work = await _db.WorkExperiences
                    .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null && w.EmployerName.ToLower() == normalizedEmployer);

                if (work == null)
                {
                    // try partial match
                    work = await _db.WorkExperiences
                        .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null && w.EmployerName.ToLower().Contains(normalizedEmployer));
                }

                if (work == null)
                    return NotFound(new { Message = "Work experience not found for provided employer and candidate." });

                workId = work.Id;
            }

            var skill = new Skill
            {
                Id = Guid.NewGuid(),
                CandidateId = candidate.Id,
                WorkExperienceId = workId,
                SkillName = input.SkillName,
                SkillLevel = input.SkillLevel,
                SkillType = input.SkillType
            };

            _db.Skills.Add(skill);
            await _db.SaveChangesAsync();

            return Ok(skill);
        }

        [HttpPost("AddSoftSkill")]
        public async Task<IActionResult> AddSoftSkill([FromBody] SoftSkillInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Require either a name or a picture
            if (string.IsNullOrWhiteSpace(input.SkillName) && string.IsNullOrWhiteSpace(input.SkillPicture))
                return BadRequest(new { Message = "Either SkillName or SkillPicture must be provided." });

            var normalizedCandidateEmail = input.CandidateEmail.Trim().ToLowerInvariant();
            var candidate = await _db.Candidates
                .FirstOrDefaultAsync(c => c.EmailAddress.ToLower() == normalizedCandidateEmail);

            if (candidate == null)
                return NotFound(new { Message = "Candidate not found for provided email." });

            Guid? picId = null;
            if (!string.IsNullOrWhiteSpace(input.SkillPicture))
            {
                var pic = new Picture { Id = Guid.NewGuid(), Photo = input.SkillPicture };
                _db.Pictures.Add(pic);
                picId = pic.Id;
            }

            var soft = new SoftSkill
            {
                Id = Guid.NewGuid(),
                CandidateId = candidate.Id,
                SkillName = input.SkillName,
                SkillPictureId = picId
            };

            _db.SoftSkills.Add(soft);
            await _db.SaveChangesAsync();

            return Ok(soft);
        }
    }
}
