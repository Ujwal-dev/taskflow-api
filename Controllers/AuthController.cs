using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowAPI.Data;
using TaskFlowAPI.DTOs;
using TaskFlowAPI.Models;
using TaskFlowAPI.Services;

namespace TaskFlowAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtService  _jwt;

    public AuthController(AppDbContext db, IJwtService jwt)
    {
        _db  = db;
        _jwt = jwt;
    }

    // POST api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict(new ApiResponse<object>(false, "Email already registered.", null));

        var user = new User
        {
            FullName     = req.FullName,
            Email        = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role         = Role.User
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);

        return CreatedAtAction(nameof(Register), new ApiResponse<AuthResponse>(
            true,
            "Registration successful.",
            new AuthResponse(
                token,
                user.FullName,
                user.Email,
                user.Role.ToString(),
                DateTime.UtcNow.AddHours(24)
            )
        ));
    }

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new ApiResponse<object>(false, "Invalid email or password.", null));

        var token = _jwt.GenerateToken(user);

        return Ok(new ApiResponse<AuthResponse>(
            true,
            "Login successful.",
            new AuthResponse(
                token,
                user.FullName,
                user.Email,
                user.Role.ToString(),
                DateTime.UtcNow.AddHours(24)
            )
        ));
    }
}
