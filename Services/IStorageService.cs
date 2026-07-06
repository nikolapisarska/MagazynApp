using MagazynApp.Model;

namespace MagazynApp.Services;

public interface IStorageService
{
    Task<bool> ImportFromCsvAsync(string? filePath = null);
    Task<Product?> GetProductByCodeAsync(string code);
    Task<Box?> GetBoxByCodeAsync(string boxCode);
    Task<Box> GetOrCreateBoxAsync(string boxCode);
    Task SaveBoxAsync(Box box);
    Task ExportBoxToCsvAsync(Box box);
}