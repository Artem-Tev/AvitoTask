namespace prReviewerAppoint.Models
{
    public class User
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        
        public ICollection<Team> Teams { get; set; } = new List<Team>();
        public ICollection<PullRequest> AuthoredPRs { get; set; } = new List<PullRequest>();
        public ICollection<PrReviewer> ReviewAssignments { get; set; } = new List<PrReviewer>();
    }

    public class Team
    {
        public string TeamName { get; set; } = string.Empty;
        
        public ICollection<User> Members { get; set; } = new List<User>();
    }

    public enum PrStatus
    {
        OPEN,
        MERGED
    }

    public class PullRequest
    {
        public string PullRequestId { get; set; } = string.Empty;
        public string PullRequestName { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public PrStatus Status { get; set; } = PrStatus.OPEN;
        public DateTime? CreatedAt { get; set; }
        public DateTime? MergedAt { get; set; }
        
        public User Author { get; set; } = null!;
        public ICollection<PrReviewer> Reviewers { get; set; } = new List<PrReviewer>();
    }

    public class PrReviewer
    {
        public string PrId { get; set; } = string.Empty;
        public string ReviewerId { get; set; } = string.Empty;
        
        public PullRequest PullRequest { get; set; } = null!;
        public User Reviewer { get; set; } = null!;
    }
}
