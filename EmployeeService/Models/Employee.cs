namespace EmployeeService.Models
{
    public class Employee
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        public string Email { get; set; } = default!;

        public int DepartmentId { get; set; }

        public decimal Salary { get; set; }
    }
}

