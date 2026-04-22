using DepartmentService.Application.Common.Interfaces;
using DepartmentService.Application.DTOs;
using DepartmentService.Domain;
using DepartmentService.Domain.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Shared.Logging;

namespace DepartmentService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogSender _logger;

    public DepartmentsController(IDepartmentRepository repository, IMapper mapper, ILogSender logs)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logs;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetAll(CancellationToken ct)
    {
        await _logger.SendLogAsync("Fetching all departments");
        try 
        {
            var departments = await _repository.GetAllAsync(ct);
            var result = _mapper.Map<IEnumerable<DepartmentDto>>(departments);
            await _logger.SendLogAsync($"Successfully fetched {result.Count()} departments");
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _logger.SendLogAsync($"Failed to fetch departments: {ex.Message}", "Error", ex.ToString());
            throw;
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentDto>> GetById(int id, CancellationToken ct)
    {
        await _logger.SendLogAsync($"Fetching department with ID: {id}");
        try 
        {
            var department = await _repository.GetByIdAsync(id, ct);
            if (department == null) 
            {
                await _logger.SendLogAsync($"Department with ID {id} not found", "Warning");
                return NotFound();
            }

            await _logger.SendLogAsync($"Successfully fetched department: {department.Name}");
            return Ok(_mapper.Map<DepartmentDto>(department));
        }
        catch (Exception ex)
        {
            await _logger.SendLogAsync($"Error fetching department {id}: {ex.Message}", "Error", ex.ToString());
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create(CreateDepartmentDto dto, CancellationToken ct)
    {
        await _logger.SendLogAsync($"Creating new department: {dto.Name}");
        try 
        {
            var department = _mapper.Map<Department>(dto);
            await _repository.AddAsync(department, ct);
            await _repository.SaveChangesAsync(ct);

            var result = _mapper.Map<DepartmentDto>(department);
            await _logger.SendLogAsync($"Department created successfully with ID: {result.DepartmentId}");
            return CreatedAtAction(nameof(GetById), new { id = result.DepartmentId }, result);
        }
        catch (Exception ex)
        {
            await _logger.SendLogAsync($"Failed to create department: {ex.Message}", "Error", ex.ToString());
            throw;
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateDepartmentDto dto, CancellationToken ct)
    {
        await _logger.SendLogAsync($"Attempting to update department ID: {id}");
        try 
        {
            var department = await _repository.GetByIdAsync(id, ct);
            if (department == null) 
            {
                await _logger.SendLogAsync($"Update failed: Department {id} not found", "Warning");
                return NotFound();
            }

            _mapper.Map(dto, department);
            _repository.Update(department);
            await _repository.SaveChangesAsync(ct);

            await _logger.SendLogAsync($"Department {id} updated successfully");
            return NoContent();
        }
        catch (Exception ex)
        {
            await _logger.SendLogAsync($"Error updating department {id}: {ex.Message}", "Error", ex.ToString());
            throw;
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _logger.SendLogAsync($"Attempting to delete department ID: {id}");
        try 
        {
            var department = await _repository.GetByIdAsync(id, ct);
            if (department == null) 
            {
                await _logger.SendLogAsync($"Delete failed: Department {id} not found", "Warning");
                return NotFound();
            }

            _repository.Delete(department);
            await _repository.SaveChangesAsync(ct);

            await _logger.SendLogAsync($"Department {id} deleted successfully");
            return NoContent();
        }
        catch (Exception ex)
        {
            await _logger.SendLogAsync($"Error deleting department {id}: {ex.Message}", "Error", ex.ToString());
            throw;
        }
    }
}