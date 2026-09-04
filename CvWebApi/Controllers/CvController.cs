using CvWebApi.Data;
using CvWebApi.InputModels;
using CvWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace CvWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CvController : ControllerBase
    {
        private readonly CvDbContext _db;
        private readonly IConfiguration _config;

        public CvController(CvDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("Register")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "User registered", typeof(Candidate))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Registration failed. ModelState invalid or user exists.")]
        public async Task<IActionResult> Register([FromBody] AuthRegisterInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = input.EmailAddress?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { Message = "Email can't be null or empty" });

            var exists = await _db.Candidates.FirstOrDefaultAsync(c => c.EmailAddress == email);
            if (exists is not null)
                return BadRequest(new { Message = "User with this email already exists." });

            var candidate = new Candidate
            {
                Id = Guid.NewGuid(),
                FullName = input.FullName,
                EmailAddress = email
            };

            var hasher = new PasswordHasher<Candidate>();
            candidate.PasswordHash = hasher.HashPassword(candidate, input.Password);

            _db.Candidates.Add(candidate);
            await _db.SaveChangesAsync();

            return Ok(candidate);
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Login successful. Returns JWT token.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Login failed. Invalid credentials.")]
        public async Task<IActionResult> Login([FromBody] AuthLoginInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = input.EmailAddress?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { Message = "Email can't be null or empty" });

            var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.EmailAddress == email);
            if (candidate is null || string.IsNullOrWhiteSpace(candidate.PasswordHash))
                return BadRequest(new { Message = "Invalid credentials." });

            var hasher = new PasswordHasher<Candidate>();
            var result = hasher.VerifyHashedPassword(candidate, candidate.PasswordHash, input.Password);
            if (result == PasswordVerificationResult.Failed)
                return BadRequest(new { Message = "Invalid credentials." });

            // create token
            var jwtKey = _config.GetValue<string>("Jwt:Key");
            var jwtIssuer = _config.GetValue<string>("Jwt:Issuer");
            var jwtAudience = _config.GetValue<string>("Jwt:Audience");
            var expireMinutes = _config.GetValue<int>("Jwt:ExpireMinutes");

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "JWT signing key is not configured. Please set 'Jwt:Key' in configuration." });
            }

            var claims = new[] {
                new Claim(ClaimTypes.Email, candidate.EmailAddress),
                new Claim(ClaimTypes.NameIdentifier, candidate.Id.ToString()),
                new Claim(ClaimTypes.Name, candidate.FullName ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return Ok(new { token = tokenString });
        }

        [HttpPost("AddCv")]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "Candidate successfully added", typeof(Candidate))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Candidate not added. ModelState is invalid")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own CV")]
        public async Task<IActionResult> AddCv([FromBody] CandidateInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);            

            // Try to find existing candidate by email (case-insensitive)
            var inputEmailAddress = input.EmailAddress?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputEmailAddress))
                return BadRequest(new { Message = "Email can't be null or empty" });

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(userEmail) || userEmail != inputEmailAddress)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own CV" });

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
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "WorkExperience successfully added", typeof(WorkExperience))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Work experience not added. ModelState is invalid")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found for provided email.")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own work experience")]
        public async Task<IActionResult> AddWorkExperience([FromBody] WorkExperienceInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var inputCandidateEmail = input.CandidateEmail?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputCandidateEmail))
                return BadRequest(new { Message = "CandidateEmail can't be null or empty" });

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(userEmail) || userEmail != inputCandidateEmail)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own work experience" });

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
            {
                work = new WorkExperience()
                {
                    Id = Guid.NewGuid(),
                    EmployerName = input.EmployerName,
                    CandidateId = candidate.Id
                };

                _db.WorkExperiences.Add(work);
            }

            work.JobTitle = input.JobTitle;
            work.StartDate = input.StartDate;
            work.EndDate = input.EndDate;
            work.Location = input.Location;
            work.Summary = input.Summary;

            await _db.SaveChangesAsync();

            return Ok(work);
        }

        [HttpPost("AddAchievementAndTask")]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "Achievement and task successfully added", typeof(AchievementsAndTasks))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Achievement and task not added. Work experience not found for provided employer and candidate.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found for provided email.")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own achievements")]
        public async Task<IActionResult> AddAchievementAndTask([FromBody] AchievementAndTaskInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var inputCandidateEmail = input.CandidateEmail?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputCandidateEmail))
                return BadRequest(new { Message = "CandidateEmail can't be null or empty" });

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(userEmail) || userEmail != inputCandidateEmail)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own achievements" });

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
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "Skill successfully added", typeof(Skill))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Skill not added. ModelState is invalid")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate or WorkEexperience not found for provided employer and candidate.")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own skills")]
        public async Task<IActionResult> AddSkill([FromBody] SkillInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var inputCandidateEmail = input.CandidateEmail?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputCandidateEmail))
                return BadRequest(new { Message = "CandidateEmail can't be null or empty" });

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(userEmail) || userEmail != inputCandidateEmail)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own skills" });

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
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "SoftSkill successfully added", typeof(SoftSkill))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "SoftSkill not added. Either SkillName or SkillPicture must be provided.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found for provided email.")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own soft skills")]
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

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(userEmail) || userEmail != inputCandidateEmail)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own soft skills" });

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
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "Interest successfully added", typeof(Interest))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Interest not added. ModelState is invalid")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found for provided email.")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own interests")]
        public async Task<IActionResult> AddInterest([FromBody] InterestInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(input.InterestName) && string.IsNullOrWhiteSpace(input.InterestPicture))
                return BadRequest(new { Message = "Either InterestName or InterestPicture must be provided." });

            var inputCandidateEmail = input.CandidateEmail?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputCandidateEmail))
                return BadRequest(new { Message = "CandidateEmail can't be null or empty" });

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(userEmail) || userEmail != inputCandidateEmail)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own interests" });

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
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "Education successfully added", typeof(Education))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Education not added. ModelState is invalid")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found for provided email.")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own education records")]
        public async Task<IActionResult> AddEducation([FromBody] EducationInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var inputCandidateEmail = input.CandidateEmail?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputCandidateEmail))
                return BadRequest(new { Message = "CandidateEmail can't be null or empty" });

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(userEmail) || userEmail != inputCandidateEmail)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own education records" });

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
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "Reference successfully added", typeof(Education))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Reference not added. Work experience not found for provided employer and candidate.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found for provided email.")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own references")]
        public async Task<IActionResult> AddReference([FromBody] ReferenceInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var inputCandidateEmail = input.CandidateEmail?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(inputCandidateEmail))
                return BadRequest(new { Message = "CandidateEmail can't be null or empty" });

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(userEmail) || userEmail != inputCandidateEmail)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own references" });

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

        [HttpDelete("DeleteWorkExperience/{id}")]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "WorkExperience deleted")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "WorkExperience not found")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own work experience")]
        public async Task<IActionResult> DeleteWorkExperience([FromRoute] Guid id)
        {
            var work = await _db.WorkExperiences
                .Include(w => w.AchievementsAndTasks)
                .Include(w => w.Skills)
                .Include(w => w.References)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (work is null)
                return NotFound(new { Message = "WorkExperience not found." });

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            var owner = await _db.Candidates.FindAsync(work.CandidateId);
            if (owner == null || string.IsNullOrWhiteSpace(userEmail) || userEmail != owner.EmailAddress)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own work experience" });

            // Remove children first
            if (work.AchievementsAndTasks.Any())
                _db.AchievementsAndTasks.RemoveRange(work.AchievementsAndTasks);

            if (work.Skills.Any())
                _db.Skills.RemoveRange(work.Skills);

            if (work.References.Any())
                _db.References.RemoveRange(work.References);

            _db.WorkExperiences.Remove(work);
            await _db.SaveChangesAsync();

            return Ok(new { Message = "WorkExperience deleted.", Id = id });
        }

        [HttpDelete("DeleteAchievement/{id}")]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "Achievement deleted")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Achievement not found")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own achievements")]
        public async Task<IActionResult> DeleteAchievement([FromRoute] Guid id)
        {
            var ach = await _db.AchievementsAndTasks.FindAsync(id);
            if (ach is null)
                return NotFound(new { Message = "Achievement not found." });
            // ensure owner
            var work = await _db.WorkExperiences.FindAsync(ach.WorkExperienceId);
            if (work == null)
                return NotFound(new { Message = "Related work experience not found." });

            var owner = await _db.Candidates.FindAsync(work.CandidateId);
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (owner == null || string.IsNullOrWhiteSpace(userEmail) || userEmail != owner.EmailAddress)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own achievements" });

            _db.AchievementsAndTasks.Remove(ach);
            await _db.SaveChangesAsync();
            return Ok(new { Message = "Achievement deleted.", Id = id });
        }

        [HttpDelete("DeleteSkill/{id}")]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "Skill deleted")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Skill not found")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own skills")]
        public async Task<IActionResult> DeleteSkill([FromRoute] Guid id)
        {
            var skill = await _db.Skills.FindAsync(id);
            if (skill is null)
                return NotFound(new { Message = "Skill not found." });
            var owner = await _db.Candidates.FindAsync(skill.CandidateId);
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (owner == null || string.IsNullOrWhiteSpace(userEmail) || userEmail != owner.EmailAddress)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own skills" });

            _db.Skills.Remove(skill);
            await _db.SaveChangesAsync();
            return Ok(new { Message = "Skill deleted.", Id = id });
        }

        [HttpDelete("DeleteSoftSkill/{id}")]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "SoftSkill deleted")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "SoftSkill not found")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own soft skills")]
        public async Task<IActionResult> DeleteSoftSkill([FromRoute] Guid id)
        {
            var soft = await _db.SoftSkills.FindAsync(id);
            if (soft is null)
                return NotFound(new { Message = "SoftSkill not found." });
            var owner = await _db.Candidates.FindAsync(soft.CandidateId);
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (owner == null || string.IsNullOrWhiteSpace(userEmail) || userEmail != owner.EmailAddress)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own soft skills" });

            // remove associated picture if present
            if (soft.SkillPictureId != null)
            {
                var pic = await _db.Pictures.FindAsync(soft.SkillPictureId.Value);
                if (pic != null)
                    _db.Pictures.Remove(pic);
            }

            _db.SoftSkills.Remove(soft);
            await _db.SaveChangesAsync();
            return Ok(new { Message = "SoftSkill deleted.", Id = id });
        }

        [HttpDelete("DeleteInterest/{id}")]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "Interest deleted")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Interest not found")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own interests")]
        public async Task<IActionResult> DeleteInterest([FromRoute] Guid id)
        {
            var interest = await _db.Interests.FindAsync(id);
            if (interest is null)
                return NotFound(new { Message = "Interest not found." });
            var owner = await _db.Candidates.FindAsync(interest.CandidateId);
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (owner == null || string.IsNullOrWhiteSpace(userEmail) || userEmail != owner.EmailAddress)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own interests" });

            if (interest.InterestPictureId != null)
            {
                var pic = await _db.Pictures.FindAsync(interest.InterestPictureId.Value);
                if (pic != null)
                    _db.Pictures.Remove(pic);
            }

            _db.Interests.Remove(interest);
            await _db.SaveChangesAsync();
            return Ok(new { Message = "Interest deleted.", Id = id });
        }

        [HttpDelete("DeleteEducation/{id}")]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "Education deleted")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Education not found")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own education records")]
        public async Task<IActionResult> DeleteEducation([FromRoute] Guid id)
        {
            var edu = await _db.Educations.FindAsync(id);
            if (edu is null)
                return NotFound(new { Message = "Education not found." });
            var owner = await _db.Candidates.FindAsync(edu.CandidateId);
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (owner == null || string.IsNullOrWhiteSpace(userEmail) || userEmail != owner.EmailAddress)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own education records" });

            _db.Educations.Remove(edu);
            await _db.SaveChangesAsync();
            return Ok(new { Message = "Education deleted.", Id = id });
        }

        [HttpDelete("DeleteReference/{id}")]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "Reference deleted")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Reference not found")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own references")]
        public async Task<IActionResult> DeleteReference([FromRoute] Guid id)
        {
            var reference = await _db.References.FindAsync(id);
            if (reference is null)
                return NotFound(new { Message = "Reference not found." });
            // determine owner
            Guid? ownerCandidateId = reference.CandidateId;
            if (ownerCandidateId == null && reference.WorkExperienceId != null)
            {
                var work = await _db.WorkExperiences.FindAsync(reference.WorkExperienceId.Value);
                ownerCandidateId = work?.CandidateId;
            }

            if (ownerCandidateId == null)
                return NotFound(new { Message = "Owner not found for reference." });

            var owner = await _db.Candidates.FindAsync(ownerCandidateId.Value);
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (owner == null || string.IsNullOrWhiteSpace(userEmail) || userEmail != owner.EmailAddress)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own references" });

            _db.References.Remove(reference);
            await _db.SaveChangesAsync();
            return Ok(new { Message = "Reference deleted.", Id = id });
        }

        [HttpDelete("DeleteCv/{id}")]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "Candidate and related records deleted")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidate not found")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "You may only modify your own CV")]
        public async Task<IActionResult> DeleteCv([FromRoute] Guid id)
        {
            var candidate = await _db.Candidates.FindAsync(id);
            if (candidate is null)
                return NotFound(new { Message = "Candidate not found." });

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(userEmail) || userEmail != candidate.EmailAddress)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "You may only modify your own CV" });

            // gather related work experience ids
            var workIds = await _db.WorkExperiences.Where(w => w.CandidateId == id).Select(w => w.Id).ToListAsync();

            if (workIds.Any())
            {
                // delete achievements linked to those work experiences
                var ach = _db.AchievementsAndTasks.Where(a => workIds.Contains(a.WorkExperienceId));
                _db.AchievementsAndTasks.RemoveRange(ach);

                // delete references linked to those work experiences
                var refsByWork = _db.References.Where(r => r.WorkExperienceId != null && workIds.Contains(r.WorkExperienceId.Value));
                _db.References.RemoveRange(refsByWork);

                // delete work-related skills
                var skillsByWork = _db.Skills.Where(s => s.WorkExperienceId != null && workIds.Contains(s.WorkExperienceId.Value));
                _db.Skills.RemoveRange(skillsByWork);

                // delete the work experiences
                var works = _db.WorkExperiences.Where(w => w.CandidateId == id);
                _db.WorkExperiences.RemoveRange(works);
            }

            // delete references, skills, education, softskills, interests tied to candidate
            var refs = _db.References.Where(r => r.CandidateId == id);
            _db.References.RemoveRange(refs);

            var skills = _db.Skills.Where(s => s.CandidateId == id);
            _db.Skills.RemoveRange(skills);

            var educations = _db.Educations.Where(e => e.CandidateId == id);
            _db.Educations.RemoveRange(educations);

            var softs = await _db.SoftSkills.Where(s => s.CandidateId == id).ToListAsync();
            var softPicIds = softs.Where(s => s.SkillPictureId != null).Select(s => s.SkillPictureId!.Value).ToList();
            if (softs.Any())
                _db.SoftSkills.RemoveRange(softs);

            var interests = await _db.Interests.Where(i => i.CandidateId == id).ToListAsync();
            var interestPicIds = interests.Where(i => i.InterestPictureId != null).Select(i => i.InterestPictureId!.Value).ToList();
            if (interests.Any())
                _db.Interests.RemoveRange(interests);

            // remove any pictures associated with softskills, interests or candidate profile
            var picIds = new List<Guid>();
            if (candidate.ProfilePicId != null)
                picIds.Add(candidate.ProfilePicId.Value);
            picIds.AddRange(softPicIds);
            picIds.AddRange(interestPicIds);

            if (picIds.Any())
            {
                var pics = _db.Pictures.Where(p => picIds.Contains(p.Id));
                _db.Pictures.RemoveRange(pics);
            }

            // finally remove candidate
            _db.Candidates.Remove(candidate);
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Candidate and related records deleted.", Id = id });
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
