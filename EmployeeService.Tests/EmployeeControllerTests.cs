using EmployeeService.Controllers;
using EmployeeService.Data;
using EmployeeService.DTOs;
using EmployeeService.Models;
// using EmployeeService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace EmployeeService.Tests;


[TestFixture]
public class EmployeeControllerTests
{
    private EmployeeDbContext _dbContext = null!;
    private EmployeeController _controller = null!;


    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new EmployeeDbContext(options);

        // var publisherMock = new Mock<RabbitMqPublisher>(
        //     Mock.Of<IConfiguration>(),
        //     Mock.Of<ILogger<RabbitMqPublisher>>());

        // var loggerMock = new Mock<ILogger<EmployeeController>>();

        // _controller = new EmployeeController(
        //     _dbContext,
        //     publisherMock.Object,
        //     loggerMock.Object);

        // ✅ ALWAYS set HttpContext
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }


    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    // ✅ CREATE (POST)
    [Test]
    public async Task CreateEmployee_ReturnsCreated_AndSavesToDb()
    {
        var dto = new EmployeeCreateDto(
            "John Doe",
            "john@company.com",
            1,
            50000);

        var result = await _controller.Create(dto) as CreatedAtActionResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(201));
        Assert.That(_dbContext.Employees.Count(), Is.EqualTo(1));
    }

    // ✅ GET by ID
    [Test]
    public async Task GetEmployeeById_ReturnsEmployee()
    {
        var employee = new Employee
        {
            Name = "Jane",
            Email = "jane@company.com",
            DepartmentId = 2,
            Salary = 60000
        };

        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetById(employee.Id) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        var returned = result!.Value as EmployeeReadDto;

        Assert.That(returned!.Name, Is.EqualTo("Jane"));
    }

    // ✅ UPDATE (PUT)
    [Test]
    public async Task UpdateEmployee_UpdatesDb_ReturnsNoContent()
    {
        var employee = new Employee
        {
            Name = "Old",
            Email = "old@company.com",
            DepartmentId = 1,
            Salary = 40000
        };

        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync();

        var dto = new EmployeeUpdateDto(
            "Updated",
            "updated@company.com",
            3,
            70000);

        var result = await _controller.Update(employee.Id, dto);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(_dbContext.Employees.First().Name, Is.EqualTo("Updated"));
    }

    // ✅ DELETE
    [Test]
    public async Task DeleteEmployee_RemovesFromDb()
    {
        var employee = new Employee
        {
            Name = "DeleteMe",
            Email = "delete@company.com",
            DepartmentId = 1,
            Salary = 30000
        };

        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.Delete(employee.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(_dbContext.Employees.Count(), Is.EqualTo(0));
    }

    // ✅ INVALID ID → 404
    [Test]
    public async Task GetInvalidEmployee_ReturnsNotFound()
    {
        var result = await _controller.GetById(999);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    // ✅ VALIDATION FAILURE
    [Test]
    public async Task CreateEmployee_InvalidModel_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Name", "Required");

        var dto = new EmployeeCreateDto(
            "",
            "invalid-email",
            0,
            -100);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    // ✅ DB STATE VERIFICATION
    [Test]
    public async Task EmployeeCount_IncreasesOnCreate_DecreasesOnDelete()
    {
        var dto = new EmployeeCreateDto(
            "Counter",
            "count@company.com",
            1,
            45000);

        await _controller.Create(dto);
        Assert.That(_dbContext.Employees.Count(), Is.EqualTo(1));

        var employeeId = _dbContext.Employees.First().Id;

        await _controller.Delete(employeeId);
        Assert.That(_dbContext.Employees.Count(), Is.EqualTo(0));
    }
}