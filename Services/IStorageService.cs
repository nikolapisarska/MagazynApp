using MagazynApp.Model;

namespace MagazynApp.Services;

public interface IStorageService
{
    Task<Box?> GetBoxByCodeAsync(string boxCode);
    Task UpdateBoxAsync(Box box);
    Task LogAuditAsync(string boxCode, string sku, int oldVal, int newVal, string reason);
    Task<Product?> GetProductByCodeAsync(string code);
    Task<Box> GetOrCreateBoxAsync(string boxCode);
    Task SaveBoxAsync(Box box);
    Task<List<Box>> GetClosedBoxesContainingProductAsync(string productCode);
    Task<List<Box>> GetAllBoxesAsync();
    Task<List<Product>> GetProductsAsync();
    Task<List<Box>> GetBoxesAsync();
    Task ExportDataToFileAsync(string fileName, string content);
    Task SaveProductsAsync(List<Product> products);
    Task SaveBoxesAsync(List<Box> boxes);
    Task InitializeAsync();
}