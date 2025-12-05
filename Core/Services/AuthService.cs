using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Security;
using Microsoft.IdentityModel.Tokens;

namespace LedgerCore.Core.Services;

public class AuthService(
    IUserRepository users,
    IUnitOfWork uow,
    SymmetricSecurityKey signingKey)
    : IAuthService
{
    public async Task<User?> ValidateUserAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = await users.GetByUserNameAsync(userName, cancellationToken);
        if (user is null)
            return null;

        if (user.Status != UserStatus.Active)
            return null;

        if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            return null;

        // به‌روزرسانی آخرین زمان ورود
        user.LastLoginAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(cancellationToken);

        return user;
    }

    public Task<string> GenerateJwtTokenAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var handler = new JwtSecurityTokenHandler();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("displayName", user.DisplayName ?? string.Empty),
            new("email", user.Email ?? string.Empty)
        };

        if (user.UserRoles is not null)
        {
            foreach (var userRole in user.UserRoles)
            {
                if (!string.IsNullOrWhiteSpace(userRole.Role?.Name))
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole.Role!.Name));
                }
            }
        }

        // اینجا دیگه SecurityTokenDescriptor استفاده نمی‌کنیم
        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(8), // مدت اعتبار توکن
            signingCredentials: new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256)
        );

        var jwt = handler.WriteToken(token);
        return Task.FromResult(jwt);
    }
    

    // ===== کمک‌ها برای Hash کردن پسورد =====

    private static bool VerifyPassword(
        string password,
        byte[] storedHash,
        byte[] storedSalt)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (storedHash is null || storedHash.Length == 0)
            return false;

        if (storedSalt is null || storedSalt.Length == 0)
            return false;

        using var hmac = new HMACSHA512(storedSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

        // مقایسه به صورت constant-time برای امنیت بیشتر
        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }

    // این متد برای ساخت Hash اولیه (مثلاً برای seed کردن یوزر ادمین) است
    public static void CreatePasswordHash(
        string password,
        out byte[] hash,
        out byte[] salt)
    {
        using var hmac = new HMACSHA512();
        salt = hmac.Key;
        hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }
}