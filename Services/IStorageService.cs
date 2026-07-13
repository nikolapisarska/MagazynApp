using MagazynApp.Model;

namespace MagazynApp.Services;

public interface IStorageService
{
    Task<Product?> GetProductByCodeAsync(string code);
    Task<Box> GetOrCreateBoxAsync(string boxCode);
    Task SaveBoxAsync(Box box);
    Task<Box?> GetBoxByCodeAsync(string boxCode);
    Task<List<Box>> GetClosedBoxesContainingProductAsync(string productCode);
    Task<List<Box>> GetAllBoxesAsync();
    Task<List<Product>> GetProductsAsync();
    Task<List<Box>> GetBoxesAsync();
    Task ExportDataToFile(string fileName, string content);
}