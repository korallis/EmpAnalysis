using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EmpAnalysis.Shared.Models;

namespace EmpAnalysis.Shared.Data;

public class EmpAnalysisDbContext : IdentityDbContext<Employee>
{
    public EmpAnalysisDbContext(DbContextOptions<EmpAnalysisDbContext> options)
        : base(options)
    {
    }

    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<Screenshot> Screenshots { get; set; }
    public DbSet<WebsiteVisit> WebsiteVisits { get; set; }
    public DbSet<ApplicationUsage> ApplicationUsages { get; set; }
    public DbSet<FileAccessLog> FileAccesses { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Employee relationships
        builder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        });

        // Configure ActivityLog
        builder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.Timestamp });
            entity.HasIndex(e => e.Timestamp);
            
            entity.HasOne(e => e.Employee)
                  .WithMany(e => e.ActivityLogs)
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.ProductivityScore).HasPrecision(5, 2);
        });

        // Configure Screenshot
        builder.Entity<Screenshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.CapturedAt });
            entity.HasIndex(e => e.CapturedAt);
            
            entity.HasOne(e => e.Employee)
                  .WithMany(e => e.Screenshots)
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CompressionQuality).HasPrecision(5, 2);
        });

        // Configure WebsiteVisit
        builder.Entity<WebsiteVisit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.VisitStart });
            entity.HasIndex(e => e.Domain);
            entity.HasIndex(e => e.VisitStart);
            
            entity.HasOne(e => e.Employee)
                  .WithMany(e => e.WebsiteVisits)
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ApplicationUsage
        builder.Entity<ApplicationUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.StartTime });
            entity.HasIndex(e => e.ApplicationName);
            entity.HasIndex(e => e.StartTime);
            
            entity.HasOne(e => e.Employee)
                  .WithMany(e => e.ApplicationUsages)
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.ProductivityScore).HasPrecision(5, 2);
            entity.Property(e => e.CpuUsage).HasPrecision(5, 2);
        });

        // Configure FileAccessLog
        builder.Entity<FileAccessLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.AccessTime });
            entity.HasIndex(e => e.FilePath);
            entity.HasIndex(e => e.AccessTime);
            
            entity.HasOne(e => e.Employee)
                  .WithMany(e => e.FileAccesses)
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
} 