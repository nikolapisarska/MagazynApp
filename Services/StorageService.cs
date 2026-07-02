using MagazynApp.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MagazynApp.Services;

public class StorageService
{
    // Instancja bazy danych SQLite
    private readonly StorageDb _db = new();

    public async Task<bool> ImportFromCsvAsync(string filePath)
    {
        // 1. Sprawdzenie poprawności pliku
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) 
            return false;

        var productsToImport = new List<Product>();

        try
        {
            // 2. Bezpieczne otwarcie pliku 
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(fileStream);

            // Ominięcie nagłówka (CodeOrIdGraffiti;Name)
            if (!reader.EndOfStream) 
                await reader.ReadLineAsync();

            // 3. Odczyt linia po linii
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) 
                    continue;

                var parts = line.Split(';');
                if (parts.Length >= 2)
                {
                    productsToImport.Add(new Product
                    {
                        CodeOrIdGraffiti = parts[0].Trim(),
                        Name = parts[1].Trim()
                    });
                }
            }

            // 4. Zapis listy produktów do bazy SQLite przy użyciu transakcji
            if (productsToImport.Count > 0)
            {
                await _db.ImportProductsAsync(productsToImport);
                return true;
            }

            return false;
        }
        catch (IOException)
        {
            // Obsługa błędu, gdy plik jest otwarty i zablokowany przez inny program 
            return false;
        }
        catch (Exception)
        {
            // Obsługa innych nieprzewidzianych błędów 
            return false;
        }
    }

    // Szukanie towaru po kodzie ze skanera
    public async Task<Product?> GetProductByCodeAsync(string code)
    {
        return await _db.GetProductByCodeAsync(code);
    }

    // Pobranie lub stworzenie nowego kartonu z bazy
    public async Task<Box> GetOrCreateBoxAsync(string boxCode)
    {
        var existingBox = await _db.GetBoxByCodeAsync(boxCode);
        
        // Jeśli karton już istnieje w SQLite, baza automatycznie odtworzy listę obiektów BoxItem z zapisanego stringa JSON
        if (existingBox != null)
        {
            return existingBox;
        }

        return new Box { BoxCode = boxCode };
    }

    // Zapisanie zmodyfikowanego kartonu (waga, wymiary, zawartość) do SQLite
    public async Task SaveBoxAsync(Box box)
    {
        await _db.SaveBoxAsync(box);
    }
}
