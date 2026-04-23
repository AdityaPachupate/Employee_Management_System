using MassTransit;
using Shared.DTOs;
using DepartmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DepartmentService.Domain.Entities;
using Shared.Utilities;

namespace DepartmentService.Infrastructure.Messaging
{
    public class EmployeeEventConsumer : IConsumer<EmployeeEvent>
    {
        private readonly DepartmentDbContext _dbContext;
        private readonly ILogger<EmployeeEventConsumer> _logger;

        public EmployeeEventConsumer(DepartmentDbContext dbContext, ILogger<EmployeeEventConsumer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<EmployeeEvent> context)
        {
            var employeeData = context.Message;

            _logger.LogInformation(">>> [EVENT RECEIVED] Employee Event: {Action} for Employee ID {ID}, Dept: {DeptId}",
                employeeData.EventType, employeeData.EmployeeId, employeeData.DepartmentId);

            try
            {
                var departmentExists = await _dbContext.Departments.AnyAsync(d => d.DepartmentId == employeeData.DepartmentId);

                if (!departmentExists)
                {
                    _logger.LogWarning("!!! [CRITICAL] Stats cannot be updated. Department {DeptId} does not exist in DepartmentDb.",
                        employeeData.DepartmentId);
                    return; // Stop here to avoid a Foreign Key exception
                }

                // 1. LOOK for the department statistics in our database (New Department).
                var stats = await _dbContext.DepartmentStats
                    .FirstOrDefaultAsync(s => s.DepartmentId == employeeData.DepartmentId);

                // 2. CREATE stats if they don't exist yet for this department.
                if (stats == null)
                {
                    _logger.LogInformation(">>> Creating new Stats record for Department {DeptId}", employeeData.DepartmentId);
                    stats = new DepartmentStats
                    {
                        DepartmentId = employeeData.DepartmentId,
                        EmployeeCount = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                    _dbContext.DepartmentStats.Add(stats);
                }

                // 3. UPDATE the count based on what happened (Created, Deleted, Updated)
                if (employeeData.EventType == EmployeeEventType.Created)
                {
                    stats.EmployeeCount++; // increment count for new employee
                }
                else if (employeeData.EventType == EmployeeEventType.Deleted)
                {
                    stats.EmployeeCount = Math.Max(0, stats.EmployeeCount - 1); // decrement count
                }
                else if (employeeData.EventType == EmployeeEventType.Updated)
                {
                    // Check if department has changed
                    if (employeeData.OldDepartmentId.HasValue && employeeData.OldDepartmentId.Value != employeeData.DepartmentId)
                    {
                        _logger.LogInformation(">>> [DEPT CHANGE] Employee {Id} moved from {OldDept} to {NewDept}",
                            employeeData.EmployeeId, employeeData.OldDepartmentId.Value, employeeData.DepartmentId);

                        // Decrement count for OLD department
                        var oldStats = await _dbContext.DepartmentStats
                            .FirstOrDefaultAsync(s => s.DepartmentId == employeeData.OldDepartmentId.Value);
                        
                        if (oldStats != null)
                        {
                            oldStats.EmployeeCount = Math.Max(0, oldStats.EmployeeCount - 1);
                            oldStats.LastUpdated = DateTime.UtcNow;
                        }

                        // Increment count for NEW department
                        stats.EmployeeCount++;
                    }
                }

                // 4. SAVE the changes to the database.
                stats.LastUpdated = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(">>> [SUCCESS] Updated Stats for Department {DeptId}. New Count: {Count}",
                    employeeData.DepartmentId, stats.EmployeeCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing message for Department {DeptId}", employeeData.DepartmentId);
                throw;
            }
        }
    }
}
