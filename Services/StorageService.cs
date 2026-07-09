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
                new Product { Name = "Bompka ładna", CodeOrIdGraffiti = "12345" },
                new Product { Name = "Bompka ladniejsza", CodeOrIdGraffiti = "54321" },
                new Product { Name = "Bompka zwykla taka", CodeOrIdGraffiti = "99999" },
                new Product { Name = "Bompka taka inna bo Szklana Czerwona 10cm", CodeOrIdGraffiti = "2137" },
                new Product { Name = "Trzymak bompek", CodeOrIdGraffiti = "6767" },
                new Product { Name = "Lancuch choinkowy zloty 2m", CodeOrIdGraffiti = "55555" },
                new Product { Name = "Gwiazda na czubek choinki LED", CodeOrIdGraffiti = "44444" },
                new Product { Name = "Lampki choinkowe 100pk cieple", CodeOrIdGraffiti = "33333" },
                new Product { Name = "Stojak na choinke zielony", CodeOrIdGraffiti = "11111" },
                new Product { Name = "Stojak na choinke niebieski", CodeOrIdGraffiti = "2121" },
                new Product { Name = "meow", CodeOrIdGraffiti = "meow" },
                new Product { Name = "0000", CodeOrIdGraffiti = "0000" },
                new Product {Name = "Bompka", CodeOrIdGraffiti = "2005" },
                new Product {Name = "Bompka2", CodeOrIdGraffiti = "2004" },
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(products, options);
            await _fileService.SaveFileAsync(fileName, json);
        }
    }

    public async Task<Product?> GetProductByCodeAsync(string code)
    {
        if (_productCache == null) 
            _productCache = await _fileService.LoadAllProductsAsync();
            
        return _productCache?.FirstOrDefault(p => p.CodeOrIdGraffiti == code);
    }

    public async Task<Box> GetOrCreateBoxAsync(string boxCode) => 
        (await _fileService.LoadBoxAsync(boxCode)) ?? new Box { BoxCode = boxCode };

    public async Task SaveBoxAsync(Box box) => await _fileService.SaveFileAsync($"box_{box.BoxCode}.json", JsonSerializer.Serialize(box));

    public async Task<Box?> GetBoxByCodeAsync(string boxCode) => await _fileService.LoadBoxAsync(boxCode);
}