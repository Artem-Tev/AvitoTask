using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prReviewerAppoint.Data;
using prReviewerAppoint.DTOs;
using prReviewerAppoint.Models;
using prReviewerAppoint.Services;

namespace prReviewerAppoint.Controllers
{
    [ApiController]
    [Route("pullRequest")]
    public class PullRequestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IReviewerAssignmentService _reviewerService;

        public PullRequestController(AppDbContext context, IReviewerAssignmentService reviewerService)
        {
            _context = context;
            _reviewerService = reviewerService;
        }

        [HttpPost("create")]
        public async Task<ActionResult<PullRequestResponse>> CreatePullRequest([FromBody] CreatePullRequestRequest request)
        {
            var existingPr = await _context.PullRequests.FindAsync(request.PullRequestId);
            if (existingPr != null)
            {
                return Conflict(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.PrExists,
                        Message = "PR id already exists"
                    }
                });
            }

            var author = await _context.Users
                .Include(u => u.Teams)
                .FirstOrDefaultAsync(u => u.UserId == request.AuthorId);

            if (author == null || !author.Teams.Any())
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

            var pr = new PullRequest
            {
                PullRequestId = request.PullRequestId,
                PullRequestName = request.PullRequestName,
                AuthorId = request.AuthorId,
                Status = PrStatus.OPEN,
                CreatedAt = DateTime.UtcNow
            };

            _context.PullRequests.Add(pr);
            await _context.SaveChangesAsync();

            await _reviewerService.AssignReviewersAsync(request.AuthorId, request.PullRequestId);

            pr = await _context.PullRequests
                .Include(pr => pr.Reviewers)
                    .ThenInclude(r => r.Reviewer)
                .FirstOrDefaultAsync(p => p.PullRequestId == request.PullRequestId);

            return StatusCode(201, new PullRequestResponse
            {
                Pr = MapToDto(pr!)
            });
        }

        [HttpPost("merge")]
        public async Task<ActionResult<PullRequestResponse>> MergePullRequest([FromBody] MergePullRequestRequest request)
        {
            var pr = await _context.PullRequests.FindAsync(request.PullRequestId);
            if (pr == null)
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

            if (pr.Status != PrStatus.MERGED)
            {
                pr.Status = PrStatus.MERGED;
                pr.MergedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            pr = await _context.PullRequests
                .Include(pr => pr.Reviewers)
                    .ThenInclude(r => r.Reviewer)
                .FirstOrDefaultAsync(p => p.PullRequestId == request.PullRequestId);

            return Ok(new PullRequestResponse
            {
                Pr = MapToDto(pr!)
            });
        }

        [HttpPost("reassign")]
        public async Task<ActionResult<ReassignReviewerResponse>> ReassignReviewer([FromBody] ReassignReviewerRequest request)
        {
            var pr = await _context.PullRequests
                .Include(pr => pr.Reviewers)
                .FirstOrDefaultAsync(pr => pr.PullRequestId == request.PullRequestId);

            if (pr == null)
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

            if (pr.Status == PrStatus.MERGED)
            {
                return Conflict(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.PrMerged,
                        Message = "cannot reassign on merged PR"
                    }
                });
            }

            var oldReviewer = pr.Reviewers.FirstOrDefault(r => r.ReviewerId == request.OldUserId);
            if (oldReviewer == null)
            {
                return Conflict(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.NotAssigned,
                        Message = "reviewer is not assigned to this PR"
                    }
                });
            }

            try
            {
                var newReviewerId = await _reviewerService.ReassignReviewerAsync(request.PullRequestId, request.OldUserId);

                if (string.IsNullOrEmpty(newReviewerId))
                {
                    return Conflict(new ErrorResponse
                    {
                        Error = new ErrorDetail
                        {
                            Code = ErrorCodes.NoCandidate,
                            Message = "no active replacement candidate in team"
                        }
                    });
                }

                pr = await _context.PullRequests
                    .Include(pr => pr.Reviewers)
                        .ThenInclude(r => r.Reviewer)
                    .FirstOrDefaultAsync(p => p.PullRequestId == request.PullRequestId);

                return Ok(new ReassignReviewerResponse
                {
                    Pr = MapToDto(pr!),
                    ReplacedBy = newReviewerId
                });
            }
            catch (ArgumentException)
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
        }

        private static PullRequestDto MapToDto(PullRequest pr)
        {
            return new PullRequestDto
            {
                PullRequestId = pr.PullRequestId,
                PullRequestName = pr.PullRequestName,
                AuthorId = pr.AuthorId,
                Status = pr.Status.ToString(),
                AssignedReviewers = pr.Reviewers.Select(r => r.ReviewerId).ToList(),
                CreatedAt = pr.CreatedAt,
                MergedAt = pr.MergedAt
            };
        }
    }
}

