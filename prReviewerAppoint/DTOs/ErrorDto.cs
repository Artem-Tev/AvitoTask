namespace prReviewerAppoint.DTOs
{
    public class ErrorResponse
    {
        public ErrorDetail Error { get; set; } = null!;
    }

    public class ErrorDetail
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public static class ErrorCodes
    {
        public const string TeamExists = "TEAM_EXISTS";
        public const string PrExists = "PR_EXISTS";
        public const string PrMerged = "PR_MERGED";
        public const string NotAssigned = "NOT_ASSIGNED";
        public const string NoCandidate = "NO_CANDIDATE";
        public const string NotFound = "NOT_FOUND";
    }
}

