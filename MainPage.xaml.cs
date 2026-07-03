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
    private void OnScanEntryCompleted(object sender, EventArgs e)
    {
        // Wykorzystujemy krótki moment opóźnienia (80 ms), 
        // aby system operacyjny zdążył przetworzyć pojawienie się sekcji wymiarów,
        // a następnie brutalnie kradniemy fokus z powrotem do skanera.
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(80), () =>
        {
            ScanEntry.Focus();
        });
    }

    private async void OnSaveAndCloseClicked(object sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            await vm.SaveAndCloseBoxAsync();
            
            // Po zapisaniu i zamknięciu kartonu, przywracamy fokus na skaner
            ScanEntry.Focus();
        }
    }
}