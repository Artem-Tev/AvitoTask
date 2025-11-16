using Microsoft.EntityFrameworkCore;
using prReviewerAppoint.Data;
using prReviewerAppoint.Models;

namespace prReviewerAppoint.Services
{
    public interface IReviewerAssignmentService
    {
        Task<List<string>> AssignReviewersAsync(string authorId, string prId);
        Task<string?> ReassignReviewerAsync(string prId, string oldReviewerId);
    }

    public class ReviewerAssignmentService : IReviewerAssignmentService
    {
        private readonly AppDbContext _context;
        private readonly Random _random = new();

        public ReviewerAssignmentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> AssignReviewersAsync(string authorId, string prId)
        {
            var author = await _context.Users
                .Include(u => u.Teams)
                .FirstOrDefaultAsync(u => u.UserId == authorId);

            if (author == null)
                throw new ArgumentException("Author not found", nameof(authorId));

            var candidateReviewers = await _context.Users
                .Where(u => u.IsActive 
                    && u.UserId != authorId 
                    && u.Teams.Any(t => author.Teams.Contains(t)))
                .ToListAsync();

            var selectedReviewers = candidateReviewers
                .OrderBy(_ => _random.Next())
                .Take(2)
                .ToList();

            var assignedIds = new List<string>();
            foreach (var reviewer in selectedReviewers)
            {
                _context.PrReviewers.Add(new PrReviewer
                {
                    PrId = prId,
                    ReviewerId = reviewer.UserId
                });
                assignedIds.Add(reviewer.UserId);
            }

            await _context.SaveChangesAsync();
            return assignedIds;
        }

        public async Task<string?> ReassignReviewerAsync(string prId, string oldReviewerId)
        {
            var pr = await _context.PullRequests
                .Include(pr => pr.Reviewers)
                .FirstOrDefaultAsync(pr => pr.PullRequestId == prId);

            if (pr == null)
                throw new ArgumentException("Pull request not found", nameof(prId));

            if (pr.Status == PrStatus.MERGED)
                throw new InvalidOperationException("Cannot reassign reviewers for merged PR");

            var oldReviewer = pr.Reviewers.FirstOrDefault(r => r.ReviewerId == oldReviewerId);
            if (oldReviewer == null)
                throw new ArgumentException("Reviewer not found in PR", nameof(oldReviewerId));

            var oldReviewerUser = await _context.Users
                .Include(u => u.Teams)
                .FirstOrDefaultAsync(u => u.UserId == oldReviewerId);

            if (oldReviewerUser == null)
                throw new ArgumentException("Reviewer user not found", nameof(oldReviewerId));

            var currentReviewerIds = pr.Reviewers.Select(r => r.ReviewerId).ToList();
            var candidateReviewers = await _context.Users
                .Where(u => u.IsActive
                    && u.UserId != oldReviewerId
                    && u.UserId != pr.AuthorId
                    && !currentReviewerIds.Contains(u.UserId)
                    && u.Teams.Any(t => oldReviewerUser.Teams.Contains(t)))
                .ToListAsync();

            if (candidateReviewers.Count == 0)
                return null;

            var newReviewer = candidateReviewers[_random.Next(candidateReviewers.Count)];

            _context.PrReviewers.Remove(oldReviewer);
            _context.PrReviewers.Add(new PrReviewer
            {
                PrId = prId,
                ReviewerId = newReviewer.UserId
            });

            await _context.SaveChangesAsync();
            return newReviewer.UserId;
        }
    }
}

