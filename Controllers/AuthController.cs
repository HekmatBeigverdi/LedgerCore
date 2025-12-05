using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.ViewModels.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// ورود کاربر و دریافت JWT.
    /// POST api/auth/login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("UserName و Password الزامی هستند.");
        }

        var user = await authService.ValidateUserAsync(
            request.UserName,
            request.Password,
            cancellationToken);

        if (user is null)
        {
            return Unauthorized("نام کاربری یا رمز عبور اشتباه است.");
        }

        var token = await authService.GenerateJwtTokenAsync(user, cancellationToken);

        var response = new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Email = user.Email
        };

        return Ok(response);
    }
}