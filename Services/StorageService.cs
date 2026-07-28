using MagazynApp.Model;
using SQLite;

namespace MagazynApp.Services;

/// <summary>
/// Serwis odpowiedzialny za zarządzanie lokalną bazą danych SQLite.
/// Obsługuje operacje CRUD dla produktów, kartonów oraz logów audytowych.
/// Wykorzystuje mechanizm bezpieczny wątkowo (SemaphoreSlim oraz pełny mutex).
/// </summary>
public class StorageService : IStorageService
{
    private SQLiteAsyncConnection? _db;
    private readonly string _dbPath = Path.Combine(FileSystem.AppDataDirectory, "Magazyn.db3");
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isInitialized;

    /// <summary>
    /// Inicjalizuje połączenie z bazą danych oraz tworzy wymagane tabele, jeśli jeszcze nie istnieją.
    /// Zabezpieczony semaforem przed wielokrotnym wywołaniem równoległym.
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        await _semaphore.WaitAsync();
        try
        {
            if (!_isInitialized)
            {
                _db = new SQLiteAsyncConnection(_dbPath,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
                await _db.CreateTableAsync<Product>();
                await _db.CreateTableAsync<Box>();
                await _db.CreateTableAsync<AuditLog>();
                _isInitialized = true;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>Wyszukuje produkt w bazie na podstawie jego kodu lub ID Graffiti.</summary>
    public async Task<Product?> GetProductByCodeAsync(string code)
    {
        await EnsureInitializedAsync();
        return await _db!.Table<Product>().FirstOrDefaultAsync(p => p.CodeOrIdGraffiti == code);
    }

    /// <summary>Pobiera istniejący karton lub tworzy nowy obiekt domyślny, jeśli karton nie istnieje w bazie.</summary>
    public async Task<Box> GetOrCreateBoxAsync(string boxCode)
    {
        await EnsureInitializedAsync();
        var box = await GetBoxByCodeAsync(boxCode);
        return box ?? new Box { BoxCode = boxCode, Status = BoxStatus.InProgress, Weight = 0.0 };
    }

    /// <summary>Zapisuje lub aktualizuje karton w bazie danych (przygotowując go wcześniej do serializacji/zapisu).</summary>
    public async Task SaveBoxAsync(Box box)
    {
        await EnsureInitializedAsync();
        box.PrepareForSave();
        await _db!.InsertOrReplaceAsync(box);
    }

    /// <summary>Pobiera karton z bazy na podstawie kodu i wykonuje operacje deserializacyjne dla jego zawartości.</summary>
    public async Task<Box?> GetBoxByCodeAsync(string boxCode)
    {
        await EnsureInitializedAsync();
        var box = await _db!.Table<Box>().FirstOrDefaultAsync(b => b.BoxCode == boxCode);
        box?.LoadAfterRead();
        return box;
    }

    /// <summary>Zwraca listę zamkniętych kartonów, które zawierają wskazany produkt.</summary>
    public async Task<List<Box>> GetClosedBoxesContainingProductAsync(string productCode)
    {
        await EnsureInitializedAsync();
        var allClosed = await _db!.Table<Box>().Where(b => b.IsClosed).ToListAsync();
        foreach (var box in allClosed) box.LoadAfterRead();
        // Zmiana z i.ProductId na i.CodeOrIdGraffiti:
        return allClosed.Where(b => b.Items.Any(i => i.CodeOrIdGraffiti == productCode)).ToList();
    }

    /// <summary>Pobiera listę wszystkich kartonów z bazy danych.</summary>
    public async Task<List<Box>> GetAllBoxesAsync()
    {
        await EnsureInitializedAsync();
        var list = await _db!.Table<Box>().ToListAsync();
        foreach (var box in list) box.LoadAfterRead();
        return list;
    }

    /// <summary>Pobiera listę wszystkich produktów z bazy danych.</summary>
    public async Task<List<Product>> GetProductsAsync()
    {
        await EnsureInitializedAsync();
        return await _db!.Table<Product>().ToListAsync();
    }

    /// <summary>Metoda pomocnicza aliasująca pobieranie wszystkich kartonów.</summary>
    public async Task<List<Box>> GetBoxesAsync() => await GetAllBoxesAsync();

    /// <summary>Eksportuje wskazane dane tekstowe (np. JSON) do pliku w katalogu aplikacji.</summary>
    public async Task ExportDataToFileAsync(string fileName, string content)
    {
        string path = Path.Combine(FileSystem.AppDataDirectory, fileName);
        await File.WriteAllTextAsync(path, content);
        await Shell.Current.DisplayAlertAsync("Sukces", $"Plik zapisano w: {path}", "OK");
    }

    /// <summary>Zapisuje listę produktów do bazy danych.</summary>
    public async Task SaveProductsAsync(List<Product> products)
    {
        await EnsureInitializedAsync();
        foreach (var product in products)
        {
            await _db!.InsertOrReplaceAsync(product);
        }
    }

    /// <summary>Zapisuje listę kartonów do bazy danych.</summary>
    public async Task SaveBoxesAsync(List<Box> boxes)
    {
        await EnsureInitializedAsync();
        foreach (var box in boxes)
        {
            box.PrepareForSave();
            await _db!.InsertOrReplaceAsync(box);
        }
    }

    /// <summary>Aktualizuje dane istniejącego kartonu w bazie.</summary>
    public async Task UpdateBoxAsync(Box box)
    {
        await EnsureInitializedAsync();
        box.PrepareForSave();
        await _db!.UpdateAsync(box);
    }

    /// <summary>Zapisuje wpis w logu audytu (np. korekty ilości, braki, uszkodzenia).</summary>
    public async Task LogAuditAsync(string boxCode, string sku, int oldVal, int newVal, string reason)
    {
        await EnsureInitializedAsync();
        var log = new AuditLog
        {
            BoxCode = boxCode,
            Sku = sku,
            OldQuantity = oldVal,
            NewQuantity = newVal,
            Reason = reason
        };
        await _db!.InsertAsync(log);

        System.Diagnostics.Debug.WriteLine($"[AUDIT ZAPISANO] {log.Description}");
    }

    /// <summary>Jawna inicjalizacja bazy danych (np. wywoływana przy starcie aplikacji).</summary>
    public async Task InitializeAsync()
    {
        await EnsureInitializedAsync();
    }
}