namespace LedgerCore.Core.Interfaces;

/// <summary>
/// Abstraction over current time for testability.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime Today { get; }
}