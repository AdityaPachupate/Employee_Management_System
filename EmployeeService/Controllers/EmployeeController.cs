using EmployeeService.Data;
using EmployeeService.DTOs;
using EmployeeService.Models;
using EmployeeService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace EmployeeService.Controllers
{
    [ApiController]
    [Route("employees")]
    public class EmployeeController : ControllerBase
    {
        private readonly EmployeeDbContext _db;
        private readonly RabbitMqPublisher _publisher;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(
            EmployeeDbContext db,
            RabbitMqPublisher publisher,
            ILogger<EmployeeController> logger)
        {
            _db = db;
            _publisher = publisher;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var employees = await _db.Employees
                .Select(e => new EmployeeReadDto(
                    e.Id,
                    e.Name,
                    e.Email,
                    e.DepartmentId,
                    e.Salary))
                .ToListAsync();

            return Ok(employees);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var employee = await _db.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            return Ok(new EmployeeReadDto(
                employee.Id,
                employee.Name,
                employee.Email,
                employee.DepartmentId,
                employee.Salary));
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeCreateDto dto)
        {
            var employee = new Employee
            {
                Name = dto.Name,
                Email = dto.Email,
                DepartmentId = dto.DepartmentId,
                Salary = dto.Salary
            };

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();

            _publisher.PublishEmployeeCreated(employee, GetCorrelationId());

            return CreatedAtAction(
                nameof(GetById),
                new { id = employee.Id },
                new EmployeeReadDto(
                    employee.Id,
                    employee.Name,
                    employee.Email,
                    employee.DepartmentId,
                    employee.Salary));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, EmployeeUpdateDto dto)
        {
            var employee = await _db.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            employee.Name = dto.Name;
            employee.Email = dto.Email;
            employee.DepartmentId = dto.DepartmentId;
            employee.Salary = dto.Salary;

            await _db.SaveChangesAsync();

            _publisher.PublishEmployeeUpdated(employee, GetCorrelationId());

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _db.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            _db.Employees.Remove(employee);
            await _db.SaveChangesAsync();

            _publisher.PublishEmployeeDeleted(employee, GetCorrelationId());

            return NoContent();
        }

        private string GetCorrelationId()
            => Request.Headers["X-Correlation-Id"].FirstOrDefault()
               ?? Guid.NewGuid().ToString();
    }
}

