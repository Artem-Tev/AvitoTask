using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prReviewerAppoint.Data;
using prReviewerAppoint.DTOs;
using prReviewerAppoint.Models;

namespace prReviewerAppoint.Controllers
{
    [ApiController]
    [Route("team")]
    public class TeamController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TeamController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("add")]
        public async Task<ActionResult<TeamResponse>> AddTeam([FromBody] TeamDto dto)
        {
            var existingTeam = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamName == dto.TeamName);

            if (existingTeam != null)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.TeamExists,
                        Message = "team_name already exists"
                    }
                });
            }

            var team = new Team
            {
                TeamName = dto.TeamName
            };

            foreach (var memberDto in dto.Members)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == memberDto.UserId);

                if (user == null)
                {
                    user = new User
                    {
                        UserId = memberDto.UserId,
                        Username = memberDto.Username,
                        IsActive = memberDto.IsActive
                    };
                    _context.Users.Add(user);
                }
                else
                {
                    user.Username = memberDto.Username;
                    user.IsActive = memberDto.IsActive;
                }

                team.Members.Add(user);
            }

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamName == dto.TeamName);

            return StatusCode(201, new TeamResponse
            {
                Team = MapToDto(team!)
            });
        }

        [HttpGet("get")]
        public async Task<ActionResult<TeamDto>> GetTeam([FromQuery] string team_name)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamName == team_name);

            if (team == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.NotFound,
                        Message = "resource not found"
                    }
                });
            }

            return Ok(MapToDto(team));
        }

        private static TeamDto MapToDto(Team team)
        {
            return new TeamDto
            {
                TeamName = team.TeamName,
                Members = team.Members.Select(m => new TeamMemberDto
                {
                    UserId = m.UserId,
                    Username = m.Username,
                    IsActive = m.IsActive
                }).ToList()
            };
        }
    }
}

