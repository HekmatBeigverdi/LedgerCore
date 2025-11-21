using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Payroll;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IPayrollRepository : IRepository<PayrollDocument>
{
    Task<PayrollDocument?> GetWithLinesAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<PayrollDocument>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}