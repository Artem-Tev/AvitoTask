namespace prReviewerAppoint.DTOs
{
    public class TeamMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class TeamDto
    {
        public string TeamName { get; set; } = string.Empty;
        public List<TeamMemberDto> Members { get; set; } = new();
    }

    public class TeamResponse
    {
        public TeamDto Team { get; set; } = null!;
    }

    public class UserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class UserResponse
    {
        public UserDto User { get; set; } = null!;
    }

    public class SetIsActiveRequest
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class PullRequestDto
    {
        public string PullRequestId { get; set; } = string.Empty;
        public string PullRequestName { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> AssignedReviewers { get; set; } = new();
        
        [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("mergedAt")]
        public DateTime? MergedAt { get; set; }
    }

    public class PullRequestShortDto
    {
        public string PullRequestId { get; set; } = string.Empty;
        public string PullRequestName { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class PullRequestResponse
    {
        public PullRequestDto Pr { get; set; } = null!;
    }

    public class CreatePullRequestRequest
    {
        public string PullRequestId { get; set; } = string.Empty;
        public string PullRequestName { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
    }

    public class MergePullRequestRequest
    {
        public string PullRequestId { get; set; } = string.Empty;
    }

    public class ReassignReviewerRequest
    {
        public string PullRequestId { get; set; } = string.Empty;
        public string OldUserId { get; set; } = string.Empty;
    }

    public class ReassignReviewerResponse
    {
        public PullRequestDto Pr { get; set; } = null!;
        public string ReplacedBy { get; set; } = string.Empty;
    }

    public class GetReviewResponse
    {
        public string UserId { get; set; } = string.Empty;
        public List<PullRequestShortDto> PullRequests { get; set; } = new();
    }
}

