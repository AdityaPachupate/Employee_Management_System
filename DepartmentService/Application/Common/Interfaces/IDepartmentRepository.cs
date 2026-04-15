using System;
using DepartmentService.Domain.Entities;

namespace DepartmentService.Application.Common.Interfaces;

public interface IDepartmentRepository
{
    Task<IEnumerable<Department>> GetAllAsync(CancellationToken ct = default);
    Task<Department?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task AddAsync(Department department, CancellationToken ct = default);
    void Update(Department department);
    void Delete(Department department);
    Task UpdateStatsAsync(int departmentId, int delta, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
