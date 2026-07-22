using System.Diagnostics;
using MagazynApp.ViewModels;

namespace MagazynApp;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        await _viewModel.InitializeLocalDatabaseAsync();
        
        // Wymuszenie skupienia z małym opóźnieniem dla stabilności na desktopie i mobile
        await Task.Delay(250);
        ScanEntry.Focus();
    }

    private async void OnSaveAndCloseClicked(object? sender, EventArgs e)
    {
        Debug.WriteLine("DEBUG: Przycisk Zapisz i Zamknij kliknięty.");
        await _viewModel.SaveAndReturnAsync();
        
        await Task.Delay(100);
        ScanEntry.Focus();
    }
}