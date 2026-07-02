using SQLite;
using MagazynApp.Model;
using System.IO;

namespace MagazynApp.Services;

public class StorageDb
{
    private SQLiteAsyncConnection? _database;

    private async Task InitAsync()
    {
        if (_database != null) return;

        // Automatyczna ścieżka do bazy danych, niezależna od systemu 
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "magazyn.db3");
        
        _database = new SQLiteAsyncConnection(dbPath);
        
        // Tworzenie tabel, jeśli jeszcze nie istnieją
        await _database.CreateTableAsync<Product>();
        await _database.CreateTableAsync<Box>();
    }
    
    // OPERACJE NA PRODUKTACH (Z CSV) 
    public async Task ImportProductsAsync(List<Product> products)
    {
        await InitAsync();
        
        // Uruchamiamy transakcję asynchronicznie, ale w środku (w pętli)
        // używamy synchronicznej metody 'tran.InsertOrReplace', ponieważ
        // silnik bazy danych blokuje wątek na czas całej transakcji dla wydajności.
        await _database!.RunInTransactionAsync(tran =>
        {
            foreach (var prod in products)
            {
                tran.InsertOrReplace(prod); 
            }
        });
    }

    public async Task<Product?> GetProductByCodeAsync(string code)
    {
        await InitAsync();
        return await _database!.Table<Product>()
            .Where(p => p.CodeOrIdGraffiti == code)
            .FirstOrDefaultAsync();
    }

    // OPERACJE NA KARTONACH (SKANOWANIE I WYSZUKIWANIE)
    public async Task<Box?> GetBoxByCodeAsync(string boxCode)
    {
        await InitAsync();
        return await _database!.Table<Box>()
            .Where(b => b.BoxCode == boxCode)
            .FirstOrDefaultAsync();
    }

    public async Task SaveBoxAsync(Box box)
    {
        await InitAsync();
        await _database!.InsertOrReplaceAsync(box);
    }
}