using DepartmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DepartmentService.Infrastructure.Persistence;

public class DepartmentDbContext : DbContext
{
    public DepartmentDbContext(DbContextOptions<DepartmentDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<DepartmentStats> DepartmentStats { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Department entity
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // Configure DepartmentStats entity
        modelBuilder.Entity<DepartmentStats>(entity =>
        {
            entity.HasKey(e => e.DepartmentId);
            
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // 1-to-1 relationship with Department
            entity.HasOne(ds => ds.Department)
                .WithOne()
                .HasForeignKey<DepartmentStats>(ds => ds.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
