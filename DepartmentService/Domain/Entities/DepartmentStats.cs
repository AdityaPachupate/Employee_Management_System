using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DepartmentService.Domain.Entities;

public class DepartmentStats
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int DepartmentId { get; set; }

    public int EmployeeCount { get; set; } = 0;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual Department? Department { get; set; }
}
