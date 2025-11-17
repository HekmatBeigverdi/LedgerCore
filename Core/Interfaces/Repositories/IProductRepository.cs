using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IProductRepository: IRepository<Product>
{
    Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}