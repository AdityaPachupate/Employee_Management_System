namespace Shared.DTOs;

public enum EmployeeEventType
{
    Created = 1,
    Updated = 2,
    Deleted = 3
}

public class EmployeeEvent
{
    public int EmployeeId { get; set; }
    public int DepartmentId { get; set; }
    public decimal Salary { get; set; }
    public EmployeeEventType EventType { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
