using MagazynApp.Model;

namespace MagazynApp.Services;

public interface IStorageService
{
  
    Task<Box?> GetBoxByCode(string boxCode);
    Task Update(Box box);
    Task UpdateBox(Box box); // Dodano
    Task LogAudit(string boxCode, string sku, int oldVal, int newVal, string reason); // Dodano

    Task<Product?> GetProductByCodeAsync(string code);
    Task<Box> GetOrCreateBoxAsync(string boxCode);
    Task SaveBoxAsync(Box box);
    Task<Box?> GetBoxByCodeAsync(string boxCode);
    Task<List<Box>> GetClosedBoxesContainingProductAsync(string productCode);
    Task<List<Box>> GetAllBoxesAsync();
    Task<List<Product>> GetProductsAsync();
    Task<List<Box>> GetBoxesAsync();
    Task ExportDataToFile(string fileName, string content);
    Task SaveProductsAsync(List<Product> products);
    Task SaveBoxesAsync(List<Box> boxes);
    Task InitializeAsync();
}