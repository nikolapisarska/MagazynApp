using System;
using MagazynApp.ViewModels;
using Microsoft.Maui.Controls;

namespace MagazynApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        // Sprawdź, czy w pliku XAML nie masz przypisanego <vm:MainViewModel /> w BindingContext,
        // jeśli masz, tę linię poniżej można zakomentować, żeby nie tworzyć ViewModelu dwa razy.
        BindingContext = new MainViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Ustawienie fokusu nax
        //pole skanera, aby magazynier mógł od razu działać
        ScanEntry.Focus();

        // Jednorazowe, automatyczne załadowanie wbudowanego pliku CSV przy starcie aplikacji
        if (BindingContext is MainViewModel vm)
        {
            // Odpalamy import z zasobów lokalnych (bez podawania ścieżki sieciowej)
            await vm.InitializeLocalDatabaseAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    // Ta metoda wykonuje się automatycznie, gdy użytkownik/skaner kliknie Enter
    // Dodaj znak zapytania 'object?' przy parametrze sender
    private void OnScanEntryCompleted(object? sender, EventArgs e)
    {
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(80), () =>
        {
            ScanEntry.Focus();
        });
    }

    // Dodaj znak zapytania 'object?' przy parametrze sender
    private async void OnSaveAndCloseClicked(object? sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            await vm.SaveAndCloseBoxAsync();
            
            ScanEntry.Focus();
        }
    }
    private async void OnBarcodeDetected(string barcode)
    {
        if (BindingContext is MainViewModel vm)
        {
            // 1. Ustawiamy wartość w ViewModelu
            vm.ScanInput = barcode;
        
            // 2. Wywołujemy metodę bezpośrednio
            await vm.ExecuteProcessScanAsync();
        
            // 3. Opcjonalnie: ustawiamy fokus na pole skanera po zakończeniu
            ScanEntry.Focus();
        }
    }
}