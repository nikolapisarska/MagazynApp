using System.Text;
using MagazynApp.Model;

namespace MagazynApp.Services;

// Klasa pośrednicząca (Serwis), zarządzająca operacjami na danych
public class StorageService : IStorageService
{
    // Inicjalizacja instancji klasy obsługującej bazę danych SQLite
    private readonly StorageDb _db = new();

    // Metoda importująca produkty z pliku CSV (z zasobów aplikacji lub zewnętrznej ścieżki)
    public async Task<bool> ImportFromCsvAsync(string? filePath = null)
    {
        var productsToImport = new List<Product>();

        try
        {
            Stream stream;

            // Jeśli podano ścieżkę do zewnętrznego pliku, spróbuj go otworzyć
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                if (!File.Exists(filePath))
                    return false;

                stream = File.OpenRead(filePath);
            }
            else
            {
                // W przeciwnym razie pobierz plik "produkty.csv" z zasobów zainstalowanej aplikacji
                stream = await FileSystem.OpenAppPackageFileAsync("produkty.csv");
            }

            // Otwarcie strumienia pliku do odczytu
            using (stream)
            {
                using var reader = new StreamReader(stream);

                // Pominięcie pierwszej linii (nagłówek CSV z nazwami kolumn)
                await reader.ReadLineAsync();

                string? line;
                // Czytanie pliku linia po linii, aż do końca
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    // Pomiń puste linie
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Podział linii na części za pomocą średnika (separator w CSV)
                    var parts = line.Split(';');
                    if (parts.Length >= 2)
                    {
                        // Mapowanie danych z CSV na obiekt klasy Product
                        productsToImport.Add(new Product
                        {
                            CodeOrIdGraffiti = parts[0].Trim(),
                            Name = parts[1].Trim()
                        });
                    }
                }
            }

            // Jeśli zaimportowano jakiekolwiek dane, przekaż je do bazy danych
            if (productsToImport.Count > 0)
            {
                await _db.ImportProductsAsync(productsToImport);
                return true;
            }

            return false;
        }
        catch (Exception) // W razie błędu pliku lub formatu zwróć fałsz
        {
            return false;
        }
    }

    // Pobiera informacje o produkcie na podstawie kodu
    public async Task<Product?> GetProductByCodeAsync(string code)
    {
        return await _db.GetProductByCodeAsync(code);
    }

    // Pobiera istniejący karton z bazy lub tworzy nowy obiekt, jeśli nie istnieje
    public async Task<Box> GetOrCreateBoxAsync(string boxCode)
    {
        var existingBox = await _db.GetBoxByCodeAsync(boxCode);
        if (existingBox != null)
        {
            return existingBox;
        }

        // Zwraca nowy obiekt kartonu, jeśli w bazie nie znaleziono dopasowania
        return new Box { BoxCode = boxCode };
    }

    // Zapisuje zmiany w obiekcie kartonu do bazy danych
    public async Task SaveBoxAsync(Box box)
    {
        await _db.SaveBoxAsync(box);
    }

    // Bezpośrednie pobranie kartonu z bazy danych po jego kodzie
    public async Task<Box?> GetBoxByCodeAsync(string boxCode)
    {
        return await _db.GetBoxByCodeAsync(boxCode);
    }

    public async Task ExportBoxToCsvAsync(Box box)
    {
        // Nazwa pliku musi być stała, aby działało dopisywanie (Append)
        string fileName = "Historia_Kartonow.csv"; 
        string folderPath = FileSystem.AppDataDirectory; 
        string fullPath = Path.Combine(folderPath, fileName);

        var sb = new StringBuilder();
        bool fileExists = File.Exists(fullPath);

        if (!fileExists)
        {
            sb.AppendLine("BoxCode;ProductSku;ProductName;Quantity;Weight;Dimensions");
        }

        foreach (var item in box.Items)
        {
            sb.AppendLine($"{box.BoxCode};{item.ProductSku};{item.ProductName};{item.Quantity};{box.Weight};{box.Length}x{box.Width}x{box.Height}");
        }

        // Używamy dopisywania
        await File.AppendAllTextAsync(fullPath, sb.ToString());
    
        System.Diagnostics.Debug.WriteLine($"DEBUG_LOG: Zapisano do: {fullPath}");
    }
}