using MagazynApp.ViewModels;

namespace MagazynApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Ustawienie fokusu na pole skanera, aby magazynier mógł od razu działać
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

    private async void OnSaveAndCloseClicked(object sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            await vm.SaveAndCloseBoxAsync();
            
            // Po zapisaniu kartonu, przywracamy fokus na skaner
            ScanEntry.Focus();
        }
    }
}