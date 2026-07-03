using MagazynApp.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage; 

namespace MagazynApp.Services;

public class StorageService
{
    private readonly StorageDb _db = new();

    // Parametr filePath ma teraz wartość domyślną null, co pozwala wywołać metodę bez argumentów
    public async Task<bool> ImportFromCsvAsync(string? filePath = null)
    {
        var productsToImport = new List<Product>();

        try
        {
            Stream stream;

            // SYTUACJA A: Jeśli podano ścieżkę (pobieranie automatyczne z sieci)
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                if (!File.Exists(filePath))
                    return false;

                stream = File.OpenRead(filePath);
            }
            // SYTUACJA B: Brak ścieżki (start aplikacji i czytanie wbudowanego pliku z Resources/Raw/)
            else
            {
                stream = await FileSystem.OpenAppPackageFileAsync("produkty.csv");
            }

            using (stream)
            {
                using var reader = new StreamReader(stream);
                
                // Pominięcie linii nagłówkowej (CodeOrIdGraffiti;Name)
                if (!reader.EndOfStream) 
                    await reader.ReadLineAsync();

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
            }

            if (productsToImport.Count > 0)
            {
                await _db.ImportProductsAsync(productsToImport);
                return true;
            }

            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<Product?> GetProductByCodeAsync(string code)
    {
        return await _db.GetProductByCodeAsync(code);
    }

    public async Task<Box> GetOrCreateBoxAsync(string boxCode)
    {
        var existingBox = await _db.GetBoxByCodeAsync(boxCode);
        if (existingBox != null)
        {
            return existingBox;
        }

        return new Box { BoxCode = boxCode };
    }

    public async Task SaveBoxAsync(Box box)
    {
        await _db.SaveBoxAsync(box);
    }
}