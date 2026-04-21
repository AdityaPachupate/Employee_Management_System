using AuthService.Controllers;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Shared.Logging;

namespace AuthService.Tests;

[TestFixture]
public class AuthControllerTests
{
    private AuthDbContext _context;
    private Mock<ILogSender> _logSenderMock;
    private JwtService _jwtService;
    private AuthController _controller;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AuthDbContext(options);

        _logSenderMock = new Mock<ILogSender>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Key", "THIS_IS_A_VERY_LONG_TEST_SECRET_KEY_FOR_JWT_1234567890" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" }
            })
            .Build();

        _jwtService = new JwtService(config);

        _controller = new AuthController(
            _context,
            _jwtService,
            _logSenderMock.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }
    [Test]
    public async Task Register_NewUser_ReturnsOk()
    {
        var dto = new RegisterDto
        {
            Username = "testuser",
            Password = "test@123",
            Role = "User"
        };

        var result = await _controller.Register(dto);

        Assert.IsInstanceOf<OkObjectResult>(result);
    }
    [Test]
    public async Task Register_DuplicateUser_ReturnsBadRequest()
    {
        _context.Users.Add(new User
        {
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("test@123"),
            Role = "User"
        });
        await _context.SaveChangesAsync();

        var dto = new RegisterDto
        {
            Username = "testuser",
            Password = "test@123"
        };

        var result = await _controller.Register(dto);

        Assert.IsInstanceOf<BadRequestObjectResult>(result);
    }
    [Test]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        _context.Users.Add(new User
        {
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("test@123"),
            Role = "User"
        });
        await _context.SaveChangesAsync();

        var dto = new LoginDto
        {
            Username = "testuser",
            Password = "test@123"
        };

        var result = await _controller.Login(dto);

        Assert.IsInstanceOf<OkObjectResult>(result);
    }
    [Test]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        _context.Users.Add(new User
        {
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct@123"),
            Role = "User"
        });
        await _context.SaveChangesAsync();

        var dto = new LoginDto
        {
            Username = "testuser",
            Password = "wrong@123"
        };

        var result = await _controller.Login(dto);

        Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
    }
}
