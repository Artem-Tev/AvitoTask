using Microsoft.EntityFrameworkCore;
using prReviewerAppoint.Models;

namespace prReviewerAppoint.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<PullRequest> PullRequests { get; set; }
        public DbSet<PrReviewer> PrReviewers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).HasMaxLength(100);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.Username);
            });

            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasKey(e => e.TeamName);
                entity.Property(e => e.TeamName).HasMaxLength(200);
                entity.HasIndex(e => e.TeamName).IsUnique();
            });

            modelBuilder.Entity<User>()
                .HasMany(u => u.Teams)
                .WithMany(t => t.Members)
                .UsingEntity<Dictionary<string, object>>(
                    "UserTeam",
                    j => j.HasOne<Team>().WithMany().HasForeignKey("TeamName"),
                    j => j.HasOne<User>().WithMany().HasForeignKey("UserId"),
                    j => j.HasKey("UserId", "TeamName")
                );

            modelBuilder.Entity<PullRequest>(entity =>
            {
                entity.HasKey(e => e.PullRequestId);
                entity.Property(e => e.PullRequestId).HasMaxLength(100);
                entity.Property(e => e.PullRequestName).IsRequired().HasMaxLength(500);
                entity.Property(e => e.AuthorId).HasMaxLength(100);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.HasOne(e => e.Author)
                    .WithMany(u => u.AuthoredPRs)
                    .HasForeignKey(e => e.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PrReviewer>(entity =>
            {
                entity.HasKey(e => new { e.PrId, e.ReviewerId });
                entity.Property(e => e.PrId).HasMaxLength(100);
                entity.Property(e => e.ReviewerId).HasMaxLength(100);
                entity.HasOne(e => e.PullRequest)
                    .WithMany(pr => pr.Reviewers)
                    .HasForeignKey(e => e.PrId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Reviewer)
                    .WithMany(u => u.ReviewAssignments)
                    .HasForeignKey(e => e.ReviewerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
