using MagazynApp.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MagazynApp.Services;

public class StorageService
{
    private readonly StorageDb _db = new();

    public async Task<bool> ImportFromCsvAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) 
            return false;

        var productsToImport = new List<Product>();

        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(fileStream);

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