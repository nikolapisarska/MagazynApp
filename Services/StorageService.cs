using MagazynApp.Model;
using SQLite;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace MagazynApp.Services;

public class StorageService : IStorageService
{
    private SQLiteAsyncConnection? _db;
    private readonly string _dbPath = Path.Combine(FileSystem.AppDataDirectory, "Magazyn.db3");
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private bool _isInitialized = false;

    // W StorageService.cs, zmień EnsureInitializedAsync na to:
    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;
    
        await _semaphore.WaitAsync();
        try
        {
            if (!_isInitialized)
            {
                _db = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
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

    public async Task<Product?> GetProductByCodeAsync(string code)
    {
        await EnsureInitializedAsync();
        return await _db!.Table<Product>().FirstOrDefaultAsync(p => p.CodeOrIdGraffiti == code);
    }

    public async Task<Box> GetOrCreateBoxAsync(string boxCode)
    {
        await EnsureInitializedAsync();
        var box = await GetBoxByCodeAsync(boxCode);
        return box ?? new Box { BoxCode = boxCode, Status = BoxStatus.InProgress, Weight = 0.0 };
    }

    public async Task SaveBoxAsync(Box box)
    {
        await EnsureInitializedAsync();
        box.PrepareForSave();
        await _db!.InsertOrReplaceAsync(box);
    }

    public async Task<Box?> GetBoxByCodeAsync(string boxCode)
    {
        await EnsureInitializedAsync();
        var box = await _db!.Table<Box>().FirstOrDefaultAsync(b => b.BoxCode == boxCode);
        if (box != null) box.LoadAfterRead();
        return box;
    }

    public async Task<Box?> GetBoxByCode(string boxCode) => await GetBoxByCodeAsync(boxCode);

    public async Task<List<Box>> GetClosedBoxesContainingProductAsync(string productCode)
    {
        await EnsureInitializedAsync();
        var allClosed = await _db!.Table<Box>().Where(b => b.IsClosed).ToListAsync();
        foreach (var box in allClosed) box.LoadAfterRead();
        return allClosed.Where(b => b.Items.Any(i => i.ProductId == productCode)).ToList();
    }

    public async Task<List<Box>> GetAllBoxesAsync()
    {
        await EnsureInitializedAsync();
        var list = await _db!.Table<Box>().ToListAsync();
        foreach (var box in list) box.LoadAfterRead();
        return list;
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        await EnsureInitializedAsync();
        return await _db!.Table<Product>().ToListAsync();
    }

    public async Task<List<Box>> GetBoxesAsync() => await GetAllBoxesAsync();

    public async Task ExportDataToFile(string fileName, string content)
    {
        string path = Path.Combine(FileSystem.AppDataDirectory, fileName);
        await File.WriteAllTextAsync(path, content);
        await Shell.Current.DisplayAlert("Sukces", $"Plik zapisano w: {path}", "OK");
    }

    public async Task SaveProductsAsync(List<Product> products)
    {
        await EnsureInitializedAsync();
        foreach (var product in products)
        {
            await _db!.InsertOrReplaceAsync(product);
        }
    }

    public async Task SaveBoxesAsync(List<Box> boxes)
    {
        await EnsureInitializedAsync();
        foreach (var box in boxes)
        {
            box.PrepareForSave();
            await _db!.InsertOrReplaceAsync(box);
        }
    }

    public async Task Update(Box box)
    {
        await EnsureInitializedAsync();
        box.PrepareForSave();
        await _db!.UpdateAsync(box);
    }

    public async Task UpdateBox(Box box) => await Update(box);

    // ZAKTUALIZOWANA METODA LOGOWANIA
    public async Task LogAudit(string boxCode, string sku, int oldVal, int newVal, string reason)
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
    public async Task InitializeAsync()
    {
        // Tutaj np. tworzysz tabele, jeśli nie istnieją
        // await Database.CreateTableAsync<Product>();
        // await Database.CreateTableAsync<Box>();
        
        // Jeśli nie musisz nic robić, zostaw po prostu:
        await Task.CompletedTask; 
    }
    public async Task GenerateBoxReport(Box box)
    {
        // Ustawienie licencji (dla społeczności/testów)
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Header().Text($"Raport Kartonu: {box.BoxCode}").FontSize(20).Bold();
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(100);
                        columns.RelativeColumn();
                        columns.ConstantColumn(60);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Produkt");
                        header.Cell().Text("Status");
                        header.Cell().Text("Ilość");
                    });

                    foreach (var item in box.Items)
                    {
                        table.Cell().Text(item.ProductName);
                        table.Cell().Text(item.StatusLabel);
                        table.Cell().Text(item.ExpectedVsConfirmed);
                    }
                });
                page.Footer().Text(x => { x.Span("Strona "); x.CurrentPageNumber(); });
            });
        });

        // Zapis do pliku
        string path = Path.Combine(FileSystem.AppDataDirectory, $"{box.BoxCode}_Raport.pdf");
        document.GeneratePdf(path);
    
        await Shell.Current.DisplayAlert("Sukces", $"Raport PDF wygenerowany: {path}", "OK");
    }
}