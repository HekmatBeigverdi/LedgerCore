using LedgerCore.Core.Models.Payroll;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IPayrollRepository: IRepository<PayrollDocument>
{
    Task<PayrollDocument?> GetWithLinesAsync(int id, CancellationToken cancellationToken = default);

}