using DepartmentService.Application.Common.Interfaces;
using DepartmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DepartmentService.Infrastructure.Persistence.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly DepartmentDbContext _context;
    private readonly ILogger<DepartmentRepository> _logger;

    public DepartmentRepository(DepartmentDbContext context, ILogger<DepartmentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Department>> GetAllAsync(CancellationToken ct = default)
        => await _context.Departments
            .Include(d => d.Stats)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<Department?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Departments
            .Include(d => d.Stats)
            .FirstOrDefaultAsync(d => d.DepartmentId == id, ct);

    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => await _context.Departments.AnyAsync(d => d.DepartmentId == id, ct);

    public async Task AddAsync(Department department, CancellationToken ct = default)
    {
        _logger.LogInformation("Repository: Adding department {Name}", department.Name);
        await _context.Departments.AddAsync(department, ct);
    }

    public void Update(Department department)
        => _context.Departments.Update(department);

    public void Delete(Department department)
        => _context.Departments.Remove(department);

    public async Task UpdateStatsAsync(int departmentId, int delta, CancellationToken ct = default)
    {
        var stats = await _context.DepartmentStats
            .FirstOrDefaultAsync(s => s.DepartmentId == departmentId, ct);

        if (stats is null)
        {
            stats = new DepartmentStats
            {
                DepartmentId = departmentId,
                EmployeeCount = Math.Max(0, delta),
                LastUpdated = DateTime.UtcNow
            };
            await _context.DepartmentStats.AddAsync(stats, ct);
        }
        else
        {
            stats.EmployeeCount = Math.Max(0, stats.EmployeeCount + delta);
            stats.LastUpdated = DateTime.UtcNow;
            _context.DepartmentStats.Update(stats);
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}