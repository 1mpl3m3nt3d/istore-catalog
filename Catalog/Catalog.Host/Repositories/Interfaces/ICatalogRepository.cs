using Catalog.Host.Data;
using Catalog.Host.Data.Entities;

namespace Catalog.Host.Repositories.Interfaces;

public interface ICatalogRepository
{
    Task<IEnumerable<CatalogBrand?>?> GetBrandsAsync();

    Task<CatalogItem?> GetByIdAsync(int id);

    Task<PaginatedItems<CatalogItem?>?> GetByPageAsync(
        int pageSize,
        int pageIndex,
        int[]? brandFilter = null,
        int[]? typeFilter = null);

    Task<IEnumerable<CatalogItem?>?> GetProductsAsync();

    Task<IEnumerable<CatalogType?>?> GetTypesAsync();
}
