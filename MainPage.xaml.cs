using System;
using Microsoft.Maui.Controls;
using MagazynApp.ViewModels;

namespace MagazynApp;

public partial class MainPage : ContentPage
{
    private IDispatcherTimer? _backgroundDownloadTimer;

    public MainPage()
    {
        InitializeComponent();
        SetupBackgroundSync();
    }

    private void SetupBackgroundSync()
    {
        // Tworzymy timer powiązany z głównym wątkiem aplikacji
        _backgroundDownloadTimer = Dispatcher.CreateTimer();
        
        // ⏰ CZAS: Jak często baza ma się aktualizować w tle (np. co 15 minut)
        _backgroundDownloadTimer.Interval = TimeSpan.FromMinutes(15);
        
        // Co ma się stać, gdy licznik odliczy czas:
        _backgroundDownloadTimer.Tick += async (s, e) =>
        {
            if (BindingContext is MainViewModel vm)
            {
                // Wywołujemy pobieranie w tle
                await vm.DownloadAndImportCsvAutomaticallyAsync();
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Ustawienie fokusu na skaner
        ScanEntry.Focus();

        // 1. Wywołujemy pobranie od razu przy włączeniu aplikacji
        if (BindingContext is MainViewModel vm)
        {
            await vm.DownloadAndImportCsvAutomaticallyAsync();
        }

        // 2. Startujemy timer tła, żeby zaczął odliczać kolejne 15 minut
        _backgroundDownloadTimer?.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Zatrzymujemy zegar, jeśli użytkownik zamknie to okno aplikacji
        _backgroundDownloadTimer?.Stop();
    }

    private async void OnSaveAndCloseClicked(object sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            await vm.SaveAndCloseBoxAsync();
            ScanEntry.Focus();
        }
    }
}