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

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<DepartmentStats>(entity =>
        {
            entity.HasKey(e => e.DepartmentId);

            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(ds => ds.Department)
                .WithOne(d => d.Stats)
                .HasForeignKey<DepartmentStats>(ds => ds.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
