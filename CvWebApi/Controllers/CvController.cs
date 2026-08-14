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
            var existing = await _db.Candidates
                .FirstOrDefaultAsync(c => string.Equals(c.EmailAddress, input.EmailAddress.Trim(), StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
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

            var candidate = await _db.Candidates
                .FirstOrDefaultAsync(c => string.Equals(c.EmailAddress, input.CandidateEmail.Trim(), StringComparison.OrdinalIgnoreCase));

            if (candidate is null)
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

            var candidate = await _db.Candidates
                .FirstOrDefaultAsync(c => string.Equals(c.EmailAddress, input.CandidateEmail.Trim(), StringComparison.OrdinalIgnoreCase));

            if (candidate is null)
                return NotFound(new { Message = "Candidate not found for provided email." });


            // Find a work experience record for this candidate and employer name
            var work = await _db.WorkExperiences
                .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null 
                && w.EmployerName.Equals(input.EmployerName.Trim(), StringComparison.OrdinalIgnoreCase));

            // If exact case-insensitive match not found, try partial match (contains) with OrdinalIgnoreCase
            if (work is null)
            {
                work = await _db.WorkExperiences
                    .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null 
                    && w.EmployerName.Contains(input.EmployerName.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (work is null)
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

            var candidate = await _db.Candidates
                .FirstOrDefaultAsync(c => string.Equals(c.EmailAddress, input.CandidateEmail.Trim(), StringComparison.OrdinalIgnoreCase));

            if (candidate is null)
                return NotFound(new { Message = "Candidate not found for provided email." });

            Guid? workId = null;
            if (!string.IsNullOrWhiteSpace(input.EmployerName))
            {
                var work = await _db.WorkExperiences
                    .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null 
                    && w.EmployerName.Equals(input.EmployerName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (work is null)
                {
                    // try partial match with OrdinalIgnoreCase
                    work = await _db.WorkExperiences
                        .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null 
                        && w.EmployerName.Contains(input.EmployerName.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (work is null)
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

            var candidate = await _db.Candidates
                .FirstOrDefaultAsync(c => string.Equals(c.EmailAddress, input.CandidateEmail.Trim(), StringComparison.OrdinalIgnoreCase));

            if (candidate is null)
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

        [HttpPost("AddInterest")]
        public async Task<IActionResult> AddInterest([FromBody] InterestInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(input.InterestName) && string.IsNullOrWhiteSpace(input.InterestPicture))
                return BadRequest(new { Message = "Either InterestName or InterestPicture must be provided." });

            var candidate = await _db.Candidates
                .FirstOrDefaultAsync(c => string.Equals(c.EmailAddress, input.CandidateEmail.Trim(), StringComparison.OrdinalIgnoreCase));

            if (candidate is null)
                return NotFound(new { Message = "Candidate not found for provided email." });

            Guid? picId = null;
            if (!string.IsNullOrWhiteSpace(input.InterestPicture))
            {
                var pic = new Picture { Id = Guid.NewGuid(), Photo = input.InterestPicture };
                _db.Pictures.Add(pic);
                picId = pic.Id;
            }

            var interest = new Interest
            {
                Id = Guid.NewGuid(),
                CandidateId = candidate.Id,
                InterestName = input.InterestName,
                InterestPictureId = picId
            };

            _db.Interests.Add(interest);
            await _db.SaveChangesAsync();

            return Ok(interest);
        }

        [HttpPost("AddEducation")]
        public async Task<IActionResult> AddEducation([FromBody] EducationInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var candidate = await _db.Candidates
                .FirstOrDefaultAsync(c => string.Equals(c.EmailAddress, input.CandidateEmail.Trim(), StringComparison.OrdinalIgnoreCase));

            if (candidate is null)
                return NotFound(new { Message = "Candidate not found for provided email." });

            var edu = new Education
            {
                Id = Guid.NewGuid(),
                CandidateId = candidate.Id,
                InstituteName = input.InstituteName,
                Qualification = input.Qualification,
                StartDate = input.StartDate,
                EndDate = input.EndDate
            };

            _db.Educations.Add(edu);
            await _db.SaveChangesAsync();

            return Ok(edu);
        }

        [HttpPost("AddReference")]
        public async Task<IActionResult> AddReference([FromBody] ReferenceInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var candidate = await _db.Candidates
                .FirstOrDefaultAsync(c => string.Equals(c.EmailAddress, input.CandidateEmail.Trim(), StringComparison.OrdinalIgnoreCase));

            if (candidate is null)
                return NotFound(new { Message = "Candidate not found for provided email." });

            Guid? workId = null;
            if (!string.IsNullOrWhiteSpace(input.EmployerName))
            {
                var work = await _db.WorkExperiences
                    .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null 
                    && w.EmployerName.Equals(input.EmployerName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (work is null)
                {
                    // try partial match with OrdinalIgnoreCase
                    work = await _db.WorkExperiences
                        .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null 
                        && w.EmployerName.Contains(input.EmployerName.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (work is null)
                    return NotFound(new { Message = "Work experience not found for provided employer and candidate." });

                workId = work.Id;
            }

            var reference = new Reference
            {
                Id = Guid.NewGuid(),
                CandidateId = candidate.Id,
                Name = input.Name,
                Relationship = input.Relationship,
                EmailAddress = input.EmailAddress,
                PhoneNumber = input.PhoneNumber,
                WorkExperienceId = workId
            };

            _db.References.Add(reference);
            await _db.SaveChangesAsync();

            return Ok(reference);
        }

        [HttpGet("GetCv")]
        public async Task<IActionResult> GetCv([FromQuery] string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { Message = "Email query parameter is required." });

            var normalized = email.Trim();

            var candidate = await _db.Candidates
                .Where(c => string.Equals(c.EmailAddress, normalized, StringComparison.OrdinalIgnoreCase))
                .Select(c => new
                {
                    c.FullName,
                    c.EmailAddress,
                    c.PhoneNumber,
                    c.Location,
                    c.Title,
                    c.Summary,
                    ProfilePic = c.ProfilePic == null ? null : new { c.ProfilePic.Photo },
                    WorkExperience = c.WorkExperience.Select(w => new
                    {
                        w.EmployerName,
                        w.JobTitle,
                        w.StartDate,
                        w.EndDate,
                        w.Location,
                        w.Summary,
                        Skills = w.Skills.Select(s => new { s.SkillName, s.SkillLevel, s.SkillType, s.WorkExperienceId, s.CandidateId }).ToList(),
                        AchievementsAndTasks = w.AchievementsAndTasks.Select(a => new { a.Text }).ToList(),
                        References = w.References.Select(r => new { r.Name, r.Relationship, r.EmailAddress, r.PhoneNumber }).ToList()
                    }).ToList(),
                    Education = c.Education.Select(e => new { e.InstituteName, e.Qualification, e.StartDate, e.EndDate }).ToList(),
                    SoftSkills = c.SoftSkills.Select(s => new { s.SkillName, SkillPicture = s.SkillPicture == null ? null : new { s.SkillPicture.Photo } }).ToList(),
                    Skills = c.Skills.Select(s => new { s.SkillName, s.SkillLevel, s.SkillType, s.WorkExperienceId }).ToList(),
                    Interests = c.Interests.Select(i => new { i.InterestName, InterestPicture = i.InterestPicture == null ? null : new { i.InterestPicture.Photo } }).ToList(),
                    References = c.References.Select(r => new { r.Name, r.Relationship, r.EmailAddress, r.PhoneNumber, r.WorkExperienceId }).ToList()
                })
                .FirstOrDefaultAsync();

            if (candidate == null)
                return NotFound(new { Message = "Candidate not found for provided email." });

            return Ok(candidate);
        }
    }
}
