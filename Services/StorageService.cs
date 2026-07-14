using SQLite;
using MagazynApp.Model;

namespace MagazynApp.Services;

public class StorageService : IStorageService
{
    private SQLiteAsyncConnection? _db;
    private readonly string _dbPath = Path.Combine(FileSystem.AppDataDirectory, "Magazyn.db3");
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private bool _isInitialized = false;

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;
        await _semaphore.WaitAsync();
        try
        {
            if (!_isInitialized)
            {
                _db = new SQLiteAsyncConnection(_dbPath);
                await _db.CreateTableAsync<Product>();
                await _db.CreateTableAsync<Box>();
                _isInitialized = true;
            }
        }
        finally { _semaphore.Release(); }
    }

    public async Task<Product?> GetProductByCodeAsync(string code) 
    {
        await EnsureInitializedAsync();
        return await _db!.Table<Product>().FirstOrDefaultAsync(p => p.CodeOrIdGraffiti == code);
    }

    public async Task<Box> GetOrCreateBoxAsync(string boxCode)
    {
        await EnsureInitializedAsync();
        var box = await GetBoxByCodeAsync(boxCode);
        return box ?? new Box { BoxCode = boxCode };
    }

    public async Task SaveBoxAsync(Box box) 
    {
        await EnsureInitializedAsync();
        await _db!.InsertOrReplaceAsync(box);
    }

    public async Task<Box?> GetBoxByCodeAsync(string boxCode) 
    {
        await EnsureInitializedAsync();
        var box = await _db!.Table<Box>().FirstOrDefaultAsync(b => b.BoxCode == boxCode);
        if (box != null) box.LoadAfterRead(); 
        return box;
    }

    public async Task<List<Box>> GetClosedBoxesContainingProductAsync(string productCode)
    {
        await EnsureInitializedAsync();
        var allClosed = await _db!.Table<Box>().Where(b => b.IsClosed).ToListAsync();
        return allClosed.Where(b => b.Items.Any(i => i.ProductId == productCode)).ToList();
    }

    public async Task<List<Box>> GetAllBoxesAsync()
    {
        await EnsureInitializedAsync();

        return await _db!.Table<Box>().ToListAsync();
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        await EnsureInitializedAsync();
        return await _db!.Table<Product>().ToListAsync();
    }

    public async Task<List<Box>> GetBoxesAsync()
    {
        await EnsureInitializedAsync();
        return await _db!.Table<Box>().ToListAsync();
    }

    public async Task ExportDataToFile(string fileName, string content)
    {
        string path = Path.Combine(FileSystem.AppDataDirectory, fileName);
        await File.WriteAllTextAsync(path, content);
        // Opcjonalnie: poinformuj użytkownika
        await Shell.Current.DisplayAlert("Sukces", $"Plik zapisano w: {path}", "OK");
    }
    public async Task SaveProductsAsync(List<Product> products)
    {
        await EnsureInitializedAsync();
        foreach (var product in products)
        {
            // Jeśli Id jest 0, SQLite nada nowe Id automatycznie (dzięki AutoIncrement)
            if (product.Id == 0)
                await _db!.InsertAsync(product);
            else
                await _db!.InsertOrReplaceAsync(product);
        }
    }

    public async Task SaveBoxesAsync(List<Box> boxes)
    {
        await EnsureInitializedAsync();
        foreach (var box in boxes)
        {
            box.PrepareForSave(); // Serializuje listę Items do ItemsJson
            await _db!.InsertOrReplaceAsync(box);
        }
    }
}
