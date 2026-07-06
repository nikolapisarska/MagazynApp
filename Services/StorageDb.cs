using SQLite;
using MagazynApp.Model;

namespace MagazynApp.Services;

// Klasa obsługująca komunikację z lokalną bazą danych SQLite
public class StorageDb
{
    // Obiekt połączenia z bazą danych
    private SQLiteAsyncConnection? _database;

    // Metoda inicjalizująca bazę danych (tworzy plik i tabele, jeśli nie istnieją)
    private async Task InitAsync()
    {
        if (_database != null) return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "magazyn.db3");
    
        // DODAJ TĘ LINIĘ:
        System.Diagnostics.Debug.WriteLine($"!!! BAZA DANYCH JEST TUTAJ: {dbPath} !!!");

        _database = new SQLiteAsyncConnection(dbPath);
        await _database.CreateTableAsync<Product>();
        await _database.CreateTableAsync<Box>();
    }

    // Metoda importująca listę produktów do bazy w ramach jednej transakcji (szybciej i bezpieczniej)
    public async Task ImportProductsAsync(List<Product> products)
    {
        await InitAsync();
        // Uruchomienie operacji w transakcjijeśli coś pójdzie nie tak, zmiany zostaną wycofane
        await _database!.RunInTransactionAsync(tran =>
        {
            foreach (var prod in products)
            {
                // Dodaj nowy produkt lub zastąp istniejący, jeśli kod się powtarza
                tran.InsertOrReplace(prod);
            }
        });
    }

    // Pobiera produkt z bazy na podstawie jego unikalnego kodu
    public async Task<Product?> GetProductByCodeAsync(string code)
    {
        await InitAsync();
        return await _database!.Table<Product>()
            .Where(p => p.CodeOrIdGraffiti == code)
            .FirstOrDefaultAsync();
    }

    // Pobiera karton z bazy na podstawie jego kodu
    public async Task<Box?> GetBoxByCodeAsync(string boxCode)
    {
        await InitAsync();
        var box = await _database!.Table<Box>()
            .Where(b => b.BoxCode == boxCode)
            .FirstOrDefaultAsync();

        // Jeśli karton istnieje, wywołaj logikę deserializacji danych (np. rozpakowanie listy produktów)
        if (box != null)
        {
            box.LoadAfterRead();
        }

        return box;
    }

    // Zapisuje lub aktualizuje karton w bazie danych
    public async Task SaveBoxAsync(Box box)
    {
        await InitAsync();
        // Przygotuj obiekt do zapisu (np. serializacja listy przedmiotów do formatu bazodanowego)
        box.PrepareForSave();
        // Zapisz rekord w bazie (zastąp jeśli istnieje, dodaj jeśli nowy)
        await _database!.InsertOrReplaceAsync(box);
    }
    
}