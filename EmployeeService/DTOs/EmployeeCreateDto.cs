using System.ComponentModel.DataAnnotations;

namespace EmployeeService.DTOs;
public record EmployeeCreateDto(
    [Required] string Name,
    [Required, EmailAddress] string Email,
    [Range(1, int.MaxValue)] int DepartmentId,
    [Range(0, double.MaxValue)] decimal Salary
);

