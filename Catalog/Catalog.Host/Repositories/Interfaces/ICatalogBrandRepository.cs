namespace Catalog.Host.Repositories.Interfaces;

public interface ICatalogBrandRepository
{
    Task<int?> AddAsync(string brand);

    Task<int?> DeleteAsync(int id);

    Task<int?> UpdateAsync(int id, string brand);
}
