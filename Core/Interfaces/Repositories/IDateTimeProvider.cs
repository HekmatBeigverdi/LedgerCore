namespace LedgerCore.Core.Interfaces.Repositories;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime Today { get; }
}