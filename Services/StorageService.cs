using System.Text;
using System.Text.Json;
using MagazynApp.Model;

namespace MagazynApp.Services;

public class StorageService : IStorageService
{
    private readonly FileStorageService _fileService = new();
    private List<Product>? _productCache;

    public StorageService() => _ = InitializeMockData();

    private async Task InitializeMockData()
    {
        string fileName = "products.json";
        if (!_fileService.FileExists(fileName))
        {
            var products = new List<Product>
            {
                new() { Name = "Bompka ładna", CodeOrIdGraffiti = "12345" },
                new() { Name = "Bompka ładniejsza", CodeOrIdGraffiti = "54321" },
                new() { Name = "Bompka zwykła taka", CodeOrIdGraffiti = "99999" },
                new() { Name = "Bompka taka inna", CodeOrIdGraffiti = "2137" }
            };
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(products, options);
            await _fileService.SaveFileAsync(fileName, json);
        }    }

    public async Task<Product?> GetProductByCodeAsync(string code)
    {
        if (_productCache == null) _productCache = await _fileService.LoadAllProductsAsync();
        return _productCache.FirstOrDefault(p => p.CodeOrIdGraffiti == code);
    }

    public async Task<Box> GetOrCreateBoxAsync(string boxCode) => (await _fileService.LoadBoxAsync(boxCode)) ?? new Box { BoxCode = boxCode };

    public async Task SaveBoxAsync(Box box) => await _fileService.SaveBoxAsync(box);

    public async Task<Box?> GetBoxByCodeAsync(string boxCode) => await _fileService.LoadBoxAsync(boxCode);

    public async Task ExportBoxToCsvAsync(Box box)
    {
        string fileName = "Historia_Kartonow.csv";
        var sb = new StringBuilder();
        if (!_fileService.FileExists(fileName))
            sb.AppendLine("BoxCode;ProductSku;ProductName;Quantity;Weight;Dimensions");

        foreach (var item in box.Items)
            sb.AppendLine($"{box.BoxCode};{item.ProductSku};{item.ProductName};{item.Quantity};{box.Weight};{box.Length}x{box.Width}x{box.Height}");

        await _fileService.AppendToFileAsync(fileName, sb.ToString());
    }

    public async Task<bool> ImportFromCsvAsync(string? filePath = null) => true;
}