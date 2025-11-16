namespace prReviewerAppoint.Tests;

public partial class Program { }

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
            });
        });
    }
}

public class IntegrationTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly AppDbContext _dbContext;

    public IntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        var scope = _factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        _client.Dispose();
    }

    [Fact]
    public async Task CreateUser_ReturnsCreatedUser()
    {
        var createUserDto = new CreateUserDto
        {
            Name = "Test User",
            IsActive = true
        };

        var response = await _client.PostAsJsonAsync("/api/users", createUserDto);
        response.EnsureSuccessStatusCode();

        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        Assert.Equal("Test User", user.Name);
        Assert.True(user.IsActive);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public async Task CreateTeam_ReturnsCreatedTeam()
    {
        var createTeamDto = new CreateTeamDto
        {
            Name = "Test Team"
        };

        var response = await _client.PostAsJsonAsync("/api/teams", createTeamDto);
        response.EnsureSuccessStatusCode();

        var team = await response.Content.ReadFromJsonAsync<TeamDto>();
        Assert.NotNull(team);
        Assert.Equal("Test Team", team.Name);
    }

    [Fact]
    public async Task CreatePullRequest_AutoAssignsReviewers()
    {
        var author = await _client.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            Name = "Author",
            IsActive = true
        });
        var authorDto = await author.Content.ReadFromJsonAsync<UserDto>();

        var reviewer = await _client.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            Name = "Reviewer",
            IsActive = true
        });
        var reviewerDto = await reviewer.Content.ReadFromJsonAsync<UserDto>();

        var team = await _client.PostAsJsonAsync("/api/teams", new CreateTeamDto
        {
            Name = "Dev Team"
        });
        var teamDto = await team.Content.ReadFromJsonAsync<TeamDto>();

        await _client.PostAsJsonAsync($"/api/teams/{teamDto!.Id}/members", new AddUserToTeamDto
        {
            UserId = authorDto!.Id
        });
        await _client.PostAsJsonAsync($"/api/teams/{teamDto.Id}/members", new AddUserToTeamDto
        {
            UserId = reviewerDto!.Id
        });

        var prResponse = await _client.PostAsJsonAsync("/api/pull-requests", new CreatePullRequestDto
        {
            Title = "Test PR",
            AuthorId = authorDto.Id
        });
        prResponse.EnsureSuccessStatusCode();

        var pr = await prResponse.Content.ReadFromJsonAsync<PullRequestDto>();
        Assert.NotNull(pr);
        Assert.Equal("Test PR", pr.Title);
        Assert.True(pr.Reviewers.Count > 0, "PR should have at least one reviewer");
        Assert.Equal("Reviewer", pr.Reviewers[0].Name);
    }

    [Fact]
    public async Task MergePullRequest_IsIdempotent()
    {
        var author = await _client.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            Name = "Author",
            IsActive = true
        });
        var authorDto = await author.Content.ReadFromJsonAsync<UserDto>();

        var prResponse = await _client.PostAsJsonAsync("/api/pull-requests", new CreatePullRequestDto
        {
            Title = "Test PR",
            AuthorId = authorDto!.Id
        });
        var pr = await prResponse.Content.ReadFromJsonAsync<PullRequestDto>();

        var mergeResponse1 = await _client.PostAsJsonAsync($"/api/pull-requests/{pr!.Id}/merge", new MergePullRequestDto());
        mergeResponse1.EnsureSuccessStatusCode();
        var mergedPr1 = await mergeResponse1.Content.ReadFromJsonAsync<PullRequestDto>();
        Assert.Equal("Merged", mergedPr1!.Status);

        var mergeResponse2 = await _client.PostAsJsonAsync($"/api/pull-requests/{pr.Id}/merge", new MergePullRequestDto());
        mergeResponse2.EnsureSuccessStatusCode();
        var mergedPr2 = await mergeResponse2.Content.ReadFromJsonAsync<PullRequestDto>();
        Assert.Equal("Merged", mergedPr2!.Status);
    }

    [Fact]
    public async Task ReassignReviewer_ForMergedPR_ReturnsBadRequest()
    {
        var author = await _client.PostAsJsonAsync("/api/users", new CreateUserDto { Name = "Author", IsActive = true });
        var authorDto = await author.Content.ReadFromJsonAsync<UserDto>();
        var reviewer = await _client.PostAsJsonAsync("/api/users", new CreateUserDto { Name = "Reviewer", IsActive = true });
        var reviewerDto = await reviewer.Content.ReadFromJsonAsync<UserDto>();

        var team = await _client.PostAsJsonAsync("/api/teams", new CreateTeamDto { Name = "Team" });
        var teamDto = await team.Content.ReadFromJsonAsync<TeamDto>();
        await _client.PostAsJsonAsync($"/api/teams/{teamDto!.Id}/members", new AddUserToTeamDto { UserId = authorDto!.Id });
        await _client.PostAsJsonAsync($"/api/teams/{teamDto.Id}/members", new AddUserToTeamDto { UserId = reviewerDto!.Id });

        var prResponse = await _client.PostAsJsonAsync("/api/pull-requests", new CreatePullRequestDto
        {
            Title = "Test PR",
            AuthorId = authorDto.Id
        });
        var pr = await prResponse.Content.ReadFromJsonAsync<PullRequestDto>();
        await _client.PostAsJsonAsync($"/api/pull-requests/{pr!.Id}/merge", new MergePullRequestDto());

        var reassignResponse = await _client.PostAsJsonAsync($"/api/pull-requests/{pr.Id}/reassign-reviewer",
            new ReassignReviewerDto { OldReviewerId = reviewerDto!.Id });
        Assert.Equal(HttpStatusCode.BadRequest, reassignResponse.StatusCode);
    }
}

