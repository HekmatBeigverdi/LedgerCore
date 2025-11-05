using LedgerCore.Core.Models.Payroll;

namespace LedgerCore.Core.Interfaces.Services;

public interface IPayrollService
{
    Task<PayrollDocument> CalculatePayrollAsync(
        PayrollDocument payroll,
        CancellationToken cancellationToken = default);

    Task PostPayrollAsync(
        int payrollDocumentId,
        CancellationToken cancellationToken = default);
}