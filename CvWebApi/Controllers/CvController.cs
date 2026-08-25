using CvWebApi.Data;
using CvWebApi.InputModels;
using CvWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

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
        [SwaggerResponse(StatusCodes.Status200OK, "Candidate successfully added", typeof(Candidate))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Candidate not added. ModelState is invalid")]
        public async Task<IActionResult> AddCv([FromBody] CandidateInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Try to find existing candidate by email (case-insensitive)
            var inputEmailAddress = input.EmailAddress?.Trim().ToLower();
            if(string.IsNullOrWhiteSpace(inputEmailAddress))
                return BadRequest(new { Message = "Email can't be null or empty" });

            var existing = await _db.Candidates.FirstOrDefaultAsync(c => c.EmailAddress == inputEmailAddress);

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
                EmailAddress = inputEmailAddress,
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
        [SwaggerResponse(StatusCodes.Status200OK, "WorkExperience successfully added", typeof(WorkExperience))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Work experience not added. ModelState is invalid")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found for provided email.")]
        public async Task<IActionResult> AddWorkExperience([FromBody] WorkExperienceInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var inputCandidateEmail = input.CandidateEmail?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputCandidateEmail))
                return BadRequest(new { Message = "CandidateEmail can't be null or empty" });

            var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.EmailAddress == inputCandidateEmail);

            if (candidate is null)
                return NotFound(new { Message = "Candidate not found for provided email." });

            var work = new WorkExperience
            {
                Id = Guid.NewGuid(),
                CandidateId = candidate.Id,
                EmployerName = input.EmployerName.Trim(),
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
        [SwaggerResponse(StatusCodes.Status200OK, "Achievement and task successfully added", typeof(AchievementsAndTasks))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Achievement and task not added. Work experience not found for provided employer and candidate.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found for provided email.")]
        public async Task<IActionResult> AddAchievementAndTask([FromBody] AchievementAndTaskInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var inputCandidateEmail = input.CandidateEmail?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputCandidateEmail))
                return BadRequest(new { Message = "CandidateEmail can't be null or empty" });

            var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.EmailAddress == inputCandidateEmail);

            if (candidate is null)
                return NotFound(new { Message = "Candidate not found for provided email." });

            // Find a work experience record for this candidate and employer name
            var work = await _db.WorkExperiences
                .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null 
                && w.EmployerName.Equals(input.EmployerName.Trim()));

            // If exact case-insensitive match not found, try partial match (contains) with OrdinalIgnoreCase
            work ??= await _db.WorkExperiences
                .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null 
                && w.EmployerName.Contains(input.EmployerName.Trim()));

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
        [SwaggerResponse(StatusCodes.Status200OK, "Skill successfully added", typeof(Skill))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Skill not added. ModelState is invalid")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate or WorkEexperience not found for provided employer and candidate.")]
        public async Task<IActionResult> AddSkill([FromBody] SkillInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var inputCandidateEmail = input.CandidateEmail?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputCandidateEmail))
                return BadRequest(new { Message = "CandidateEmail can't be null or empty" });

            var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.EmailAddress == inputCandidateEmail);

            if (candidate is null)
                return NotFound(new { Message = "Candidate not found for provided email." });

            Guid? workId = null;
            if (!string.IsNullOrWhiteSpace(input.EmployerName))
            {
                var work = await _db.WorkExperiences
                    .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null 
                    && w.EmployerName.Equals(input.EmployerName.Trim()));

                work ??= await _db.WorkExperiences
                    .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null 
                    && w.EmployerName.Contains(input.EmployerName.Trim()));                

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
        [SwaggerResponse(StatusCodes.Status200OK, "SoftSkill successfully added", typeof(SoftSkill))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "SoftSkill not added. Either SkillName or SkillPicture must be provided.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found for provided email.")]
        public async Task<IActionResult> AddSoftSkill([FromBody] SoftSkillInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Require either a name or a picture
            if (string.IsNullOrWhiteSpace(input.SkillName) && string.IsNullOrWhiteSpace(input.SkillPicture))
                return BadRequest(new { Message = "Either SkillName or SkillPicture must be provided." });

            var inputCandidateEmail = input.CandidateEmail?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputCandidateEmail))
                return BadRequest(new { Message = "CandidateEmail can't be null or empty" });

            var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.EmailAddress == inputCandidateEmail);

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
        [SwaggerResponse(StatusCodes.Status200OK, "Interest successfully added", typeof(Interest))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Interest not added. ModelState is invalid")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found for provided email.")]
        public async Task<IActionResult> AddInterest([FromBody] InterestInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(input.InterestName) && string.IsNullOrWhiteSpace(input.InterestPicture))
                return BadRequest(new { Message = "Either InterestName or InterestPicture must be provided." });

            var inputCandidateEmail = input.CandidateEmail?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputCandidateEmail))
                return BadRequest(new { Message = "CandidateEmail can't be null or empty" });

            var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.EmailAddress == inputCandidateEmail);

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
        [SwaggerResponse(StatusCodes.Status200OK, "Education successfully added", typeof(Education))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Education not added. ModelState is invalid")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found for provided email.")]
        public async Task<IActionResult> AddEducation([FromBody] EducationInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var inputCandidateEmail = input.CandidateEmail?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputCandidateEmail))
                return BadRequest(new { Message = "CandidateEmail can't be null or empty" });

            var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.EmailAddress == inputCandidateEmail);

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
        [SwaggerResponse(StatusCodes.Status200OK, "Reference successfully added", typeof(Education))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Reference not added. Work experience not found for provided employer and candidate.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found for provided email.")]
        public async Task<IActionResult> AddReference([FromBody] ReferenceInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var inputCandidateEmail = input.CandidateEmail?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputCandidateEmail))
                return BadRequest(new { Message = "CandidateEmail can't be null or empty" });

            var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.EmailAddress == inputCandidateEmail);

            if (candidate is null)
                return NotFound(new { Message = "Candidate not found for provided email." });

            Guid? workId = null;
            if (!string.IsNullOrWhiteSpace(input.EmployerName))
            {
                var work = await _db.WorkExperiences
                    .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null 
                    && w.EmployerName.Equals(input.EmployerName.Trim()));

                // try partial match with OrdinalIgnoreCase
                work ??= await _db.WorkExperiences
                    .FirstOrDefaultAsync(w => w.CandidateId == candidate.Id && w.EmployerName != null
                    && w.EmployerName.Contains(input.EmployerName.Trim()));

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
        [SwaggerResponse(StatusCodes.Status200OK, "Successfully retrieved Candidate CV", typeof(Candidate))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Email query parameter is required.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found for provided email.")]
        public async Task<IActionResult> GetCv([FromQuery] string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { Message = "Email query parameter is required." });

            var normalizedEmail = email.Trim().ToLower();

            var candidate = await _db.Candidates
                .Where(c => c.EmailAddress == normalizedEmail)
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
