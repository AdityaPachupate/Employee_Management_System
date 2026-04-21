using MassTransit;
using Shared.DTOs;
using DepartmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DepartmentService.Domain.Entities;

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
            
            _logger.LogInformation(">>> Received Employee Event: {Action} for Employee ID {ID}", 
                employeeData.EventType, employeeData.EmployeeId);

            try
            {
                // 1. LOOK for the department statistics in our database.
                var stats = await _dbContext.DepartmentStats
                    .FirstOrDefaultAsync(s => s.DepartmentId == employeeData.DepartmentId);

                // 2. CREATE stats if they don't exist yet for this department.
                if (stats == null)
                {
                    stats = new DepartmentStats
                    {
                        DepartmentId = employeeData.DepartmentId,
                        EmployeeCount = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                    _dbContext.DepartmentStats.Add(stats);
                }

                // 3. UPDATE the count based on what happened (Created, Deleted, etc.)
                if (employeeData.EventType == EmployeeEventType.Created)
                {
                    stats.EmployeeCount++; // increment count for new employee
                }
                else if (employeeData.EventType == EmployeeEventType.Deleted)
                {
                    stats.EmployeeCount = Math.Max(0, stats.EmployeeCount - 1); // decrement count
                }

                // 4. SAVE the changes to the database.
                stats.LastUpdated = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(">>> Successfully updated Stats for Department {DeptId}. New Count: {Count}", 
                    employeeData.DepartmentId, stats.EmployeeCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing message for Department {DeptId}", employeeData.DepartmentId);
                // Throwing the error lets the system know it failed (so it can retry later).
                throw; 
            }
        }
    }
}
