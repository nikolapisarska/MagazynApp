using System.Collections.ObjectModel;
using System.Text.Json;
using MagazynApp.Model;
using MagazynApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Encodings.Web;

namespace MagazynApp.ViewModels;

[QueryProperty(nameof(BoxCodeToLoad), "BoxCode")]
public partial class MainViewModel(IStorageService storageService) : ObservableObject
{
    private readonly IStorageService _storageService = storageService;

    // Tekst wpisany/zeskanowany w polu wejściowym
    [ObservableProperty] private string _scanInput = string.Empty;
    
    // Komunikat informacyjny wyświetlany użytkownikowi
    [ObservableProperty] private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć lub wyszukać";
    
    // Ostatnio znaleziony produkt
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsProductVisible))] private Product? _foundProduct;
    
    // Kod kartonu przekazany w parametrach nawigacji
    [ObservableProperty] private string? _boxCodeToLoad;

    // Określa, czy panel/informacje o produkcie powinny być widoczne
    public bool IsProductVisible => FoundProduct != null;

    private Box? _currentBox;
    
    // Aktualnie obsługiwany karton w magazynie
    public Box? CurrentBox 
    {
        get => _currentBox;
        set 
        { 
            if (SetProperty(ref _currentBox, value)) 
            { 
                OnPropertyChanged(nameof(IsBoxOpen)); 
                OnPropertyChanged(nameof(IsEditable)); 
            } 
        }
    }

    // Informuje, czy karton jest aktualnie otwarty/wybrany
    public bool IsBoxOpen => CurrentBox != null;
    
    // Określa, czy karton można modyfikować (nie jest wysłany ani zamknięty)
    public bool IsEditable => CurrentBox != null && 
                              CurrentBox.Status != BoxStatus.Sent && 
                              CurrentBox.Status != BoxStatus.Closed &&
                              !CurrentBox.IsClosed;

    // Kolekcja pozycji (produktów) znajdujących się w bieżącym kartonie
    public ObservableCollection<Item> CurrentItems { get; } = [];

    // Metoda wywoływana automatycznie, gdy zmieni się kod kartonu do wczytania z zewnątrz
    partial void OnBoxCodeToLoadChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            LoadBoxByCode(value);
            BoxCodeToLoad = null;
        }
    }

    // Komenda odpowiedzialna za eksport danych (produktów lub kartonów) do pliku JSON
    [RelayCommand]
    private async Task ExportDataAsync()
    {
        try
        {
            string action = await Shell.Current.DisplayActionSheetAsync("Co chcesz wyeksportować?", "Anuluj", null, "Produkty", "Kartony");
            if (action == "Anuluj") return;

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
            };

            string json = action == "Produkty" 
                ? JsonSerializer.Serialize(await _storageService.GetProductsAsync(), options) 
                : JsonSerializer.Serialize(await _storageService.GetBoxesAsync(), options); 
            
            string fileName = $"{action}_{DateTime.Now:yyyyMMddHHmm}.json";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            
            await File.WriteAllTextAsync(filePath, json);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = $"Eksport: {action}", File = new ShareFile(filePath) });
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Błąd", ex.Message, "OK"); }
    }

    // Komenda odpowiedzialna za import danych z pliku JSON do bazy danych aplikacji
    [RelayCommand]
    private async Task ImportDataAsync()
    {
        try
        {
            string action = await Shell.Current.DisplayActionSheetAsync("Co importujesz?", "Anuluj", null, "Produkty", "Kartony");
            if (action == "Anuluj") return;
            
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Wybierz plik JSON" });
            if (result == null) return;
            
            string jsonContent = await File.ReadAllTextAsync(result.FullPath);
            if (action == "Produkty") 
                await _storageService.SaveProductsAsync(JsonSerializer.Deserialize<List<Product>>(jsonContent) ?? []);
            else 
                await _storageService.SaveBoxesAsync(JsonSerializer.Deserialize<List<Box>>(jsonContent) ?? []);
            
            await Shell.Current.DisplayAlertAsync("Sukces", "Dane zaimportowane.", "OK");
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Błąd", ex.Message, "OK"); }
    }

    // Główna logika obsługi skanera (rozpoznaje, czy zeskanowano produkt, czy kod kartonu)
    [RelayCommand]
    public async Task ProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;
        string scannedCode = ScanInput.Trim();
        ScanInput = string.Empty;

        // Krok 1: Sprawdź, czy zeskanowany kod to produkt
        var product = await _storageService.GetProductByCodeAsync(scannedCode);
        if (product != null)
        {
            FoundProduct = product;
            if (CurrentBox != null)
            {
                if (!IsEditable)
                {
                    StatusMessage = "Nie można edytować zamkniętego kartonu!";
                    return;
                }

                // Sprawdź, czy produkt już jest na liście w kartonie – jeśli tak, zwiększ ilość, jeśli nie – dodaj nowy
                // Sprawdź, czy produkt już jest na liście w kartonie – po nowym polu CodeOrIdGraffiti
                var existingItem = CurrentItems.FirstOrDefault(i => i.CodeOrIdGraffiti == product.CodeOrIdGraffiti);
                if (existingItem != null) 
                {
                    existingItem.Quantity += 1;
                }
                else
                {
                    var newItem = new Item 
                    { 
                        CodeOrIdGraffiti = product.CodeOrIdGraffiti, 
                        ProductName = product.Name, 
                        Quantity = 1 
                    };
                    CurrentItems.Add(newItem);
                    CurrentBox.Items.Add(newItem);
                    UpdateListIndices();
                }

                // Jeśli karton miał status "Nowy", zmień go na "W trakcie"
                if (CurrentBox.Status == BoxStatus.New)
                {
                    CurrentBox.Status = BoxStatus.InProgress;
                }

                await SaveCurrentBoxInternal();
                StatusMessage = $"Dodano: {product.Name}";
            }
            else
            {
                StatusMessage = $"Znaleziono: {product.Name}. Zeskanuj najpierw karton, aby dodać produkt.";
            }
            return;
        }

        // Krok 2: Sprawdź, czy zeskanowany kod to istniejący karton
        var existingBox = await _storageService.GetBoxByCodeAsync(scannedCode);
        if (existingBox != null)
        {
            await SaveCurrentBoxInternal();
            SetCurrentBox(existingBox);
            StatusMessage = $"Przełączono do kartonu: {scannedCode}. Status: {CurrentBox?.Status}";
            return;
        }

        if (CurrentBox != null && IsEditable) 
        { 
            StatusMessage = "Nie znaleziono produktu ani takiego kartonu!"; 
            return; 
        }

        // Krok 3: Jeśli kod nie pasuje do niczego, utwórz nowy karton
        var box = await _storageService.GetOrCreateBoxAsync(scannedCode);
        SetCurrentBox(box);
        StatusMessage = $"Otwarto karton: {scannedCode}. Status: {CurrentBox?.Status}";
    }

    // Komenda zapisująca bieżący karton i czyszcząca ekran do kolejnego skanowania
    [RelayCommand]
    public async Task SaveAndReturnAsync()
    {
        if (CurrentBox == null) return;
        
        string codeToReturn = CurrentBox.BoxCode;
        await SaveCurrentBoxInternal();

        CurrentBox = null; 
        CurrentItems.Clear();
        FoundProduct = null; 
        StatusMessage = $"Zapisano karton {codeToReturn}. Możesz kontynuować skanowanie.";
    }

    // Komenda usuwająca wybrany produkt z listy w aktualnym kartonie
    [RelayCommand]
    private async Task RemoveItem(Item item)
    {
        if (!IsEditable) return; 

        CurrentItems.Remove(item);
        CurrentBox?.Items.Remove(item);
        UpdateListIndices();
        await SaveCurrentBoxInternal();
    }

    // Prywatna metoda pomocnicza zapisująca stan bieżącego kartonu w usłudze magazynowej
    private async Task SaveCurrentBoxInternal()
    {
        if (CurrentBox != null) 
        { 
            CurrentBox.Items = [.. CurrentItems]; 
            CurrentBox.PrepareForSave(); 
            await _storageService.SaveBoxAsync(CurrentBox); 
        }
    }

    // Aktualizuje numery porządkowe (Lp.) oraz flagę parzystości wierszy dla interfejsu
    private void UpdateListIndices()
    {
        for (int i = 0; i < CurrentItems.Count; i++) 
        { 
            CurrentItems[i].Lp = i + 1; 
            CurrentItems[i].IsEven = (i + 1) % 2 == 0; 
        }
    }

    // Wczytuje i ustawia karton na podstawie przekazanego kodu tekstowego
    private async void LoadBoxByCode(string boxCode)
    {
        var box = await _storageService.GetBoxByCodeAsync(boxCode);
        if (box != null)
        {
            SetCurrentBox(box);
            StatusMessage = $"Otwarto karton: {boxCode}";
        }
    }

    // Przypisuje karton do zmiennej głównej i odświeża powiązane kolekcje
    private void SetCurrentBox(Box box)
    {
        CurrentBox = box;
        CurrentBox.LoadAfterRead();
        ReloadItems(CurrentBox.Items);
        FoundProduct = null;
    }
    
    // Komenda importująca listę produktów bezpośrednio do otwartego kartonu z pliku JSON
    [RelayCommand]
    private async Task ImportItemsToBoxAsync()
    {
        if (CurrentBox == null || !IsEditable)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Najpierw otwórz edytowalny karton!", "OK");
            return;
        }

        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Wybierz plik z listą produktów (JSON)" });
            if (result == null) return;

            string jsonContent = await File.ReadAllTextAsync(result.FullPath);
            var importedItems = JsonSerializer.Deserialize<List<Item>>(jsonContent);

            if (importedItems != null)
            {
                foreach (var importedItem in importedItems)
                {
                    var existingItem = CurrentItems.FirstOrDefault(i => i.CodeOrIdGraffiti == importedItem.CodeOrIdGraffiti);
                    if (existingItem != null)
                        existingItem.Quantity += importedItem.Quantity;
                    else
                        CurrentItems.Add(importedItem);
                }
            
                UpdateListIndices();
                await SaveCurrentBoxInternal();
                await Shell.Current.DisplayAlertAsync("Sukces", "Produkty zostały zaimportowane.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Błąd importu", ex.Message, "OK");
        }
    }

    // Komenda powrotu do głównego pulpitu (Dashboardu) z automatycznym zapisem stanu
    [RelayCommand]
    private async Task GoBackAsync()
    {
        await SaveCurrentBoxInternal();
        await Shell.Current.GoToAsync("///DashboardPage");
    }

    // Odświeża lokalną kolekcję elementów widoku nową listą produktów
    private void ReloadItems(IEnumerable<Item> newItems)
    {
        CurrentItems.Clear();
        foreach (var item in newItems) CurrentItems.Add(item);
        UpdateListIndices();
    }

    // Komenda przechodząca do widoku weryfikacji zawartości aktualnego kartonu
    [RelayCommand]
    private async Task GoToVerificationAsync()
    {
        if (CurrentBox == null)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Brak aktywnego kartonu do weryfikacji.", "OK");
            return;
        }

        await SaveCurrentBoxInternal();
        await Shell.Current.GoToAsync($"BoxSearchPage?ReloadBoxCode={CurrentBox.BoxCode}");
    }
}