using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeltrexAPI.Data;
using VeltrexAPI.Dtos;
using VeltrexAPI.Infrastructure;
using VeltrexAPI.Services;

namespace VeltrexAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokens, AccountService accounts) : ControllerBase
{
    /// <summary>Log in with email or User ID. The returned user's role decides which app the frontend opens.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var username = req.Username.Trim();
        var user = await db.Users.Include(u => u.Kyc).FirstOrDefaultAsync(u => u.Email == username || u.ClientCode == username, ct);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid credentials");

        return new LoginResponse(tokens.CreateToken(user), user.ToDto());
    }

    /// <summary>Create a client account and log straight in.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest req, CancellationToken ct)
    {
        var user = await accounts.RegisterClientAsync(req, ct);
        return new LoginResponse(tokens.CreateToken(user), user.ToDto());
    }

    /// <summary>The currently logged-in user (used to restore a session on page reload).</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var id = User.GetUserId();
        var user = await db.Users.AsNoTracking().Include(u => u.Kyc).FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status401Unauthorized, "Account no longer exists");
        return user.ToDto();
    }
}
