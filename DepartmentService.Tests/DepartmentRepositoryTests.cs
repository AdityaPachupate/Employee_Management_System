using DepartmentService.Domain.Entities;
using DepartmentService.Infrastructure.Persistence;
using DepartmentService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace DepartmentService.Tests;

public class DepartmentRepositoryTests : IDisposable
{
    private readonly DepartmentDbContext _context;
    private readonly DepartmentRepository _repository;
    private readonly Mock<ILogger<DepartmentRepository>> _loggerMock;

    public DepartmentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DepartmentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DepartmentDbContext(options);
        _loggerMock = new Mock<ILogger<DepartmentRepository>>();
        _repository = new DepartmentRepository(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task AddAsync_ShouldAddDepartment()
    {
        var department = new Department { Name = "IT", Description = "Tech Support" };

        await _repository.AddAsync(department);
        await _repository.SaveChangesAsync();

        var result = await _context.Departments.FirstOrDefaultAsync(d => d.Name == "IT");
        result.Should().NotBeNull();
        result!.Description.Should().Be("Tech Support");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDepartment_WhenExists()
    {
        var department = new Department { Name = "HR" };
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(department.DepartmentId);

        result.Should().NotBeNull();
        result!.Name.Should().Be("HR");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllItems()
    {
        _context.Departments.AddRange(
            new Department { Name = "IT" },
            new Department { Name = "HR" }
        );
        await _context.SaveChangesAsync();

        var results = await _repository.GetAllAsync();

        results.Should().HaveCount(2);
        results.Should().Contain(d => d.Name == "IT");
        results.Should().Contain(d => d.Name == "HR");
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(99, false)]
    public async Task ExistsAsync_ShouldReturnExpectedResult(int id, bool expected)
    {
        if (id == 1)
        {
            _context.Departments.Add(new Department { DepartmentId = 1, Name = "Test" });
            await _context.SaveChangesAsync();
        }

        var result = await _repository.ExistsAsync(id);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task UpdateStatsAsync_ShouldCreateNewStats_WhenNoneExists()
    {
        var department = new Department { Name = "Sales" };
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        await _repository.UpdateStatsAsync(department.DepartmentId, 1);
        await _repository.SaveChangesAsync();

        var stats = await _context.DepartmentStats.FirstOrDefaultAsync(s => s.DepartmentId == department.DepartmentId);
        stats.Should().NotBeNull();
        stats!.EmployeeCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateStatsAsync_ShouldUpdateExistingStats_WhenExists()
    {
        var department = new Department { Name = "Finance" };
        _context.Departments.Add(department);
        var stats = new DepartmentStats { DepartmentId = department.DepartmentId, EmployeeCount = 5 };
        _context.DepartmentStats.Add(stats);
        await _context.SaveChangesAsync();

        await _repository.UpdateStatsAsync(department.DepartmentId, 2);
        await _repository.SaveChangesAsync();

        var updatedStats = await _context.DepartmentStats.FirstOrDefaultAsync(s => s.DepartmentId == department.DepartmentId);
        updatedStats!.EmployeeCount.Should().Be(7);
    }

    [Fact]
    public async Task UpdateStatsAsync_ShouldNotAllowNegativeCount()
    {
        var department = new Department { Name = "Operations" };
        _context.Departments.Add(department);
        var stats = new DepartmentStats { DepartmentId = department.DepartmentId, EmployeeCount = 1 };
        _context.DepartmentStats.Add(stats);
        await _context.SaveChangesAsync();

        await _repository.UpdateStatsAsync(department.DepartmentId, -5);
        await _repository.SaveChangesAsync();

        var updatedStats = await _context.DepartmentStats.FirstOrDefaultAsync(s => s.DepartmentId == department.DepartmentId);
        updatedStats!.EmployeeCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_ShouldRemoveDepartment()
    {
        var department = new Department { Name = "Marketing" };
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        _repository.Delete(department);
        await _repository.SaveChangesAsync();

        var result = await _context.Departments.FindAsync(department.DepartmentId);
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
