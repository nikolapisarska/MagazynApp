using System.Text.Json;
using MagazynApp.Model;

namespace MagazynApp.Services;

public class FileStorageService
{
    private readonly string _folder;

    public FileStorageService()
    {
#if DEBUG
        if (OperatingSystem.IsWindows())
            _folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MagazynPrototypData");
        else
            _folder = FileSystem.AppDataDirectory;
#else
        _folder = FileSystem.AppDataDirectory;
#endif
        if (!Directory.Exists(_folder)) Directory.CreateDirectory(_folder);
    }

    public async Task SaveFileAsync(string fileName, string content)
    {
        var path = Path.Combine(_folder, fileName);
        await File.WriteAllTextAsync(path, content);
    }

    public async Task AppendToFileAsync(string fileName, string content)
    {
        var path = Path.Combine(_folder, fileName);
        await File.AppendAllTextAsync(path, content);
    }

    public bool FileExists(string fileName) => File.Exists(Path.Combine(_folder, fileName));

    public async Task<List<Product>> LoadAllProductsAsync()
    {
        var path = Path.Combine(_folder, "products.json");
        if (!File.Exists(path)) return new List<Product>();
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
    }

    public async Task<Box?> LoadBoxAsync(string boxCode)
    {
        var path = Path.Combine(_folder, $"box_{boxCode}.json");
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<Box>(json);
    }

    public async Task SaveBoxAsync(Box box)
    {
        var path = Path.Combine(_folder, $"box_{box.BoxCode}.json");
        var json = JsonSerializer.Serialize(box, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }
}