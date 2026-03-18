using LedgerCore.Core.Models.Security;

namespace LedgerCore.Core.Interfaces.Services;

public interface IAuthService
{
    Task<User?> ValidateUserAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default);

    Task<string> GenerateJwtTokenAsync(
        User user,
        CancellationToken cancellationToken = default);
}