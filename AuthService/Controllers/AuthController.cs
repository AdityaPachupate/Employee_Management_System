using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Logging;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly JwtService _jwtService;
        private readonly ILogSender _logger;

        public AuthController(AuthDbContext context, JwtService jwtService, ILogSender logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        // POST /auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var exists = await _context.Users
                .AnyAsync(u => u.Username == dto.Username);

            //if (exists)
            //    return BadRequest("User already exists");
            if (exists)
            {
                await _logger.SendLogAsync(
                    $"Registration attempt for existing user: {dto.Username}",
                    "Warning"
                );

                return BadRequest("User already exists");
            }

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _logger.SendLogAsync(
                $"User registered successfully: {dto.Username}",
                "Information"
            );

            return Ok("User registered successfully");

        }

        // POST /auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            //if (user == null)
            //    return Unauthorized("Invalid credentials");
            if (user == null)
            {
                await _logger.SendLogAsync(
                    $"Login failed – user not found: {dto.Username}",
                    "Warning"
                );

                return Unauthorized("Invalid credentials");
            }


            var validPassword =
                BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            //if (!validPassword)
            //    return Unauthorized("Invalid credentials");
            if (!validPassword)
            {
                await _logger.SendLogAsync(
                    $"Invalid password attempt for user: {dto.Username}",
                    "Warning"
                );

                return Unauthorized("Invalid credentials");
            }


            var token = _jwtService.GenerateToken(user);

            await _logger.SendLogAsync(
                $"User logged in successfully: {user.Username}",
                "Information"
            );


            return Ok(new { token });
        }

        [Authorize]
        [HttpGet("protected")]
        public IActionResult Protected()
        {
            return Ok("JWT token is valid");
        }
    }

}