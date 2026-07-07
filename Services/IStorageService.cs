using MagazynApp.Model;

namespace MagazynApp.Services;

public interface IStorageService
{
    Task<Product?> GetProductByCodeAsync(string code);
    Task<Box> GetOrCreateBoxAsync(string boxCode);
    Task SaveBoxAsync(Box box);
    Task<Box?> GetBoxByCodeAsync(string boxCode);
    Task ExportBoxToCsvAsync(Box box);
    Task<bool> ImportFromCsvAsync(string? filePath = null);
}