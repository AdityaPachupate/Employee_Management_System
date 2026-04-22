using DepartmentService.Application.Common.Interfaces;
using DepartmentService.Application.DTOs;
using DepartmentService.Controllers;
using DepartmentService.Domain.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Logging;
using FluentAssertions;

namespace DepartmentService.Tests;

public class DepartmentsControllerTests
{
    private readonly Mock<IDepartmentRepository> _repoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogSender> _loggerMock;
    private readonly DepartmentsController _controller;

    public DepartmentsControllerTests()
    {
        _repoMock = new Mock<IDepartmentRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogSender>();

        _controller = new DepartmentsController(_repoMock.Object, _mapperMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithData()
    {
        var departments = new List<Department> { new Department { DepartmentId = 1, Name = "IT" } };
        var dtos = new List<DepartmentDto> { new DepartmentDto { DepartmentId = 1, Name = "IT" } };
        
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(departments);
        _mapperMock.Setup(m => m.Map<IEnumerable<DepartmentDto>>(departments)).Returns(dtos);

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedItems = okResult.Value.Should().BeAssignableTo<IEnumerable<DepartmentDto>>().Subject;
        returnedItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenDepartmentDoesNotExist()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Department?)null);

        var result = await _controller.GetById(99, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenDepartmentExists()
    {
        var department = new Department { DepartmentId = 1, Name = "IT" };
        var dto = new DepartmentDto { DepartmentId = 1, Name = "IT" };

        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(department);
        _mapperMock.Setup(m => m.Map<DepartmentDto>(department)).Returns(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedItem = okResult.Value.Should().BeOfType<DepartmentDto>().Subject;
        returnedItem.DepartmentId.Should().Be(1);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction()
    {
        var createDto = new CreateDepartmentDto { Name = "New Dept" };
        var department = new Department { DepartmentId = 10, Name = "New Dept" };
        var resultDto = new DepartmentDto { DepartmentId = 10, Name = "New Dept" };

        _mapperMock.Setup(m => m.Map<Department>(createDto)).Returns(department);
        _repoMock.Setup(r => r.AddAsync(department, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<DepartmentDto>(department)).Returns(resultDto);

        var result = await _controller.Create(createDto, CancellationToken.None);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(DepartmentsController.GetById));
        createdResult.Value.Should().BeEquivalentTo(resultDto);
    }

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenSuccessful()
    {
        var id = 1;
        var updateDto = new UpdateDepartmentDto { Name = "Updated Name" };
        var department = new Department { DepartmentId = id, Name = "Old Name" };

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(department);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _repoMock.Verify(r => r.Update(It.IsAny<Department>()), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenDepartmentMissing()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Department?)null);

        var result = await _controller.Update(99, new UpdateDepartmentDto(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccessful()
    {
        var id = 1;
        var department = new Department { DepartmentId = id };

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(department);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _controller.Delete(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _repoMock.Verify(r => r.Delete(department), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenDepartmentMissing()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Department?)null);

        var result = await _controller.Delete(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetAll_ShouldLogAndThrow_WhenRepositoryFails()
    {
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new System.Exception("DB Error"));

        Func<Task> act = async () => await _controller.GetAll(CancellationToken.None);

        await act.Should().ThrowAsync<System.Exception>().WithMessage("DB Error");
        _loggerMock.Verify(l => l.SendLogAsync(It.Is<string>(s => s.Contains("Failed to fetch departments")), "Error", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Create_ShouldLogAndThrow_WhenRepositoryFails()
    {
        var createDto = new CreateDepartmentDto { Name = "Error Dept" };
        var department = new Department { Name = "Error Dept" };

        _mapperMock.Setup(m => m.Map<Department>(createDto)).Returns(department);
        _repoMock.Setup(r => r.AddAsync(department, It.IsAny<CancellationToken>())).ThrowsAsync(new System.Exception("Save Error"));

        Func<Task> act = async () => await _controller.Create(createDto, CancellationToken.None);

        await act.Should().ThrowAsync<System.Exception>().WithMessage("Save Error");
        _loggerMock.Verify(l => l.SendLogAsync(It.Is<string>(s => s.Contains("Failed to create department")), "Error", It.IsAny<string>()), Times.Once);
    }
}
