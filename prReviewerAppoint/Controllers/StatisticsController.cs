using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prReviewerAppoint.Data;
using prReviewerAppoint.DTOs;

namespace prReviewerAppoint.Controllers
{
    [ApiController]
    [Route("api/statistics")]
    public class StatisticsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatisticsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("reviewer-assignments")]
        public async Task<ActionResult<Dictionary<string, int>>> GetReviewerAssignments()
        {
            var assignments = await _context.PrReviewers
                .Include(r => r.Reviewer)
                .GroupBy(r => r.Reviewer.Username)
                .Select(g => new { ReviewerName = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ReviewerName, x => x.Count);

            return Ok(assignments);
        }

        [HttpGet("pr-statistics")]
        public async Task<ActionResult<object>> GetPrStatistics()
        {
            var totalPRs = await _context.PullRequests.CountAsync();
            var openPRs = await _context.PullRequests.CountAsync(pr => pr.Status == Models.PrStatus.OPEN);
            var mergedPRs = await _context.PullRequests.CountAsync(pr => pr.Status == Models.PrStatus.MERGED);
            var prsWithReviewers = await _context.PullRequests
                .CountAsync(pr => pr.Reviewers.Any());
            var averageReviewersPerPR = totalPRs > 0
                ? await _context.PullRequests
                    .Select(pr => pr.Reviewers.Count)
                    .AverageAsync()
                : 0;

            return Ok(new
            {
                TotalPRs = totalPRs,
                OpenPRs = openPRs,
                MergedPRs = mergedPRs,
                PRsWithReviewers = prsWithReviewers,
                AverageReviewersPerPR = Math.Round(averageReviewersPerPR, 2)
            });
        }
    }
}

