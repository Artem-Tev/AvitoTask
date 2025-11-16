using Microsoft.EntityFrameworkCore;
using prReviewerAppoint.Data;
using prReviewerAppoint.Services;
using prReviewerAppoint;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? "Host=localhost;Port=5432;Database=pr_review;Username=pguser;Password=ArtemIt2025";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IReviewerAssignmentService, ReviewerAssignmentService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending database migrations...", pendingMigrations.Count);
            db.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            db.Database.EnsureCreated();
            logger.LogInformation("Database is up to date.");
        }
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateException)
    {
        try
        {
            logger.LogInformation("Migrations table not found, ensuring database is created...");
            db.Database.EnsureCreated();
            logger.LogInformation("Database created successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create database. Please ensure PostgreSQL is running and connection string is correct.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while setting up the database. The application will continue, but database operations may fail.");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PR Reviewer API v1");
});

app.MapControllers();

app.Run();
