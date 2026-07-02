using SQLite;
using MagazynApp.Model;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MagazynApp.Services;

public class StorageDb
{
    private SQLiteAsyncConnection? _database;

    private async Task InitAsync()
    {
        if (_database != null) return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "magazyn.db3");
        _database = new SQLiteAsyncConnection(dbPath);

        await _database.CreateTableAsync<Product>();
        await _database.CreateTableAsync<Box>();
    }

    public async Task ImportProductsAsync(List<Product> products)
    {
        await InitAsync();
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

    public async Task<Box?> GetBoxByCodeAsync(string boxCode)
    {
        await InitAsync();
        var box = await _database!.Table<Box>()
            .Where(b => b.BoxCode == boxCode)
            .FirstOrDefaultAsync();

        if (box != null)
        {
            box.LoadAfterRead();
        }

        return box;
    }

    public async Task SaveBoxAsync(Box box)
    {
        await InitAsync();
        box.PrepareForSave();
        await _database!.InsertOrReplaceAsync(box);
    }
}