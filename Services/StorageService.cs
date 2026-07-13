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
        return await _db!.Table<Box>().FirstOrDefaultAsync(b => b.BoxCode == boxCode);
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
}
