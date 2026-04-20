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
        var departments = await _repository.GetAllAsync(ct);
        return Ok(_mapper.Map<IEnumerable<DepartmentDto>>(departments));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentDto>> GetById(int id, CancellationToken ct)
    {
        await _logger.SendLogAsync($"Fetching department with ID: {id}");
        var department = await _repository.GetByIdAsync(id, ct);
        if (department == null) return NotFound();

        return Ok(_mapper.Map<DepartmentDto>(department));
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create(CreateDepartmentDto dto, CancellationToken ct)
    {
        var department = _mapper.Map<Department>(dto);
        await _logger.SendLogAsync($"Creating new department: {department.Name}");
        await _repository.AddAsync(department, ct);


        await _repository.SaveChangesAsync(ct);

        var result = _mapper.Map<DepartmentDto>(department);
        return CreatedAtAction(nameof(GetById), new { id = result.DepartmentId }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateDepartmentDto dto, CancellationToken ct)
    {
        await _logger.SendLogAsync($"Updating department ID: {id}");
        var department = await _repository.GetByIdAsync(id, ct);
        if (department == null) return NotFound();

        _mapper.Map(dto, department);
        _repository.Update(department);
        await _repository.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _logger.SendLogAsync($"Deleting department ID: {id}");
        var department = await _repository.GetByIdAsync(id, ct);
        if (department == null) return NotFound();

        _repository.Delete(department);
        await _repository.SaveChangesAsync(ct);

        return NoContent();
    }
}