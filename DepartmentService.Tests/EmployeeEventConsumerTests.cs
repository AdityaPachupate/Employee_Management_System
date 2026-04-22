using DepartmentService.Infrastructure.Messaging;
using DepartmentService.Infrastructure.Persistence;
using DepartmentService.Domain.Entities;
using Shared.DTOs;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace DepartmentService.Tests;

public class EmployeeEventConsumerTests : IDisposable
{
    private readonly DepartmentDbContext _context;
    private readonly Mock<ILogger<EmployeeEventConsumer>> _loggerMock;
    private readonly EmployeeEventConsumer _consumer;

    public EmployeeEventConsumerTests()
    {
        var options = new DbContextOptionsBuilder<DepartmentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DepartmentDbContext(options);
        _loggerMock = new Mock<ILogger<EmployeeEventConsumer>>();
        _consumer = new EmployeeEventConsumer(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldIncrementCount_WhenEmployeeCreated()
    {
        var deptId = 1;
        var stats = new DepartmentStats { DepartmentId = deptId, EmployeeCount = 5 };
        _context.DepartmentStats.Add(stats);
        await _context.SaveChangesAsync();

        var message = new EmployeeEvent { DepartmentId = deptId, EmployeeId = 101, EventType = EmployeeEventType.Created };
        var contextMock = new Mock<ConsumeContext<EmployeeEvent>>();
        contextMock.Setup(c => c.Message).Returns(message);

        await _consumer.Consume(contextMock.Object);

        var updatedStats = await _context.DepartmentStats.FindAsync(deptId);
        updatedStats!.EmployeeCount.Should().Be(6);
    }

    [Fact]
    public async Task Consume_ShouldDecrementCount_WhenEmployeeDeleted()
    {
        var deptId = 2;
        var stats = new DepartmentStats { DepartmentId = deptId, EmployeeCount = 10 };
        _context.DepartmentStats.Add(stats);
        await _context.SaveChangesAsync();

        var message = new EmployeeEvent { DepartmentId = deptId, EmployeeId = 102, EventType = EmployeeEventType.Deleted };
        var contextMock = new Mock<ConsumeContext<EmployeeEvent>>();
        contextMock.Setup(c => c.Message).Returns(message);

        await _consumer.Consume(contextMock.Object);

        var updatedStats = await _context.DepartmentStats.FindAsync(deptId);
        updatedStats!.EmployeeCount.Should().Be(9);
    }

    [Fact]
    public async Task Consume_ShouldCreateStats_WhenRecordMissing()
    {
        var deptId = 3;
        var message = new EmployeeEvent { DepartmentId = deptId, EmployeeId = 103, EventType = EmployeeEventType.Created };
        var contextMock = new Mock<ConsumeContext<EmployeeEvent>>();
        contextMock.Setup(c => c.Message).Returns(message);

        await _consumer.Consume(contextMock.Object);

        var stats = await _context.DepartmentStats.FindAsync(deptId);
        stats.Should().NotBeNull();
        stats!.EmployeeCount.Should().Be(1);
    }

    [Fact]
    public async Task Consume_ShouldNotGoBelowZero_WhenDecrementing()
    {
        var deptId = 4;
        var stats = new DepartmentStats { DepartmentId = deptId, EmployeeCount = 0 };
        _context.DepartmentStats.Add(stats);
        await _context.SaveChangesAsync();

        var message = new EmployeeEvent { DepartmentId = deptId, EmployeeId = 104, EventType = EmployeeEventType.Deleted };
        var contextMock = new Mock<ConsumeContext<EmployeeEvent>>();
        contextMock.Setup(c => c.Message).Returns(message);

        await _consumer.Consume(contextMock.Object);

        var updatedStats = await _context.DepartmentStats.FindAsync(deptId);
        updatedStats!.EmployeeCount.Should().Be(0);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
