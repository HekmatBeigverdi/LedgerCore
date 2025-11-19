using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Master;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class ProductRepository(LedgerCoreDbContext context) : RepositoryBase<Product>(context), IProductRepository
{
    public Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }
}