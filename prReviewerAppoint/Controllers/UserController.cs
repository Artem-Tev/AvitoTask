using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prReviewerAppoint.Data;
using prReviewerAppoint.DTOs;

namespace prReviewerAppoint.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("setIsActive")]
        public async Task<ActionResult<UserResponse>> SetIsActive([FromBody] SetIsActiveRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Teams)
                .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (user == null)
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

            user.IsActive = request.IsActive;
            await _context.SaveChangesAsync();

            var teamName = user.Teams.FirstOrDefault()?.TeamName ?? string.Empty;

            return Ok(new UserResponse
            {
                User = new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    TeamName = teamName,
                    IsActive = user.IsActive
                }
            });
        }

        [HttpGet("getReview")]
        public async Task<ActionResult<GetReviewResponse>> GetReview([FromQuery] string user_id)
        {
            var user = await _context.Users.FindAsync(user_id);
            if (user == null)
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

            var prs = await _context.PullRequests
                .Include(pr => pr.Reviewers)
                .Where(pr => pr.Reviewers.Any(r => r.ReviewerId == user_id))
                .Select(pr => new PullRequestShortDto
                {
                    PullRequestId = pr.PullRequestId,
                    PullRequestName = pr.PullRequestName,
                    AuthorId = pr.AuthorId,
                    Status = pr.Status.ToString()
                })
                .ToListAsync();

            return Ok(new GetReviewResponse
            {
                UserId = user_id,
                PullRequests = prs
            });
        }
    }
}

