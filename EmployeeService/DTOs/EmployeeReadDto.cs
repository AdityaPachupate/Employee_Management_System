namespace EmployeeService.DTOs
{ 
    public record EmployeeReadDto(
        int Id,
        string Name,
        string Email,
        int DepartmentId,
        decimal Salary
    );

}
