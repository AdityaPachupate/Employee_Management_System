using System.ComponentModel.DataAnnotations;

namespace DepartmentService.Domain.Entities;

public class Department
{
    [Key]
    public int DepartmentId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DepartmentStats? Stats { get; set; }

}
