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

            // Try to find existing candidate by email
            var existing = await _db.Candidates
                .FirstOrDefaultAsync(c => c.EmailAddress == input.EmailAddress);

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
    }
}
