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
        
        // Wymuszenie skupienia z małym opóźnieniem dla stabilności na desktopie
        await Task.Delay(250);
        ScanEntry.Focus();
    }

    private void OnScanEntryCompleted(object sender, EventArgs e)
    {
        Debug.WriteLine("DEBUG: Zdarzenie OnScanEntryCompleted zostało wywołane.");

        if (sender is Entry entry)
        {
          
            if (string.IsNullOrWhiteSpace(entry.Text))
            {
                Debug.WriteLine("DEBUG: Entry jest puste, przerywam.");
                return;
            }

            if (_viewModel.ProcessScanCommand.CanExecute(null))
            {
                Debug.WriteLine("DEBUG: Wykonuję ProcessScanCommand.");
                _viewModel.ProcessScanCommand.Execute(null);
            }
            else
            {
                Debug.WriteLine("DEBUG: CanExecute zwróciło false.");
            }

            // Czyszczenie i ponowne ustawienie fokusu
            entry.Text = string.Empty;
            
            // Ponowne skupienie z krótkim opóźnieniem
            Dispatcher.Dispatch(async () => 
            {
                await Task.Delay(100);
                entry.Focus();
            });
        }
    }

    private async void OnSaveAndCloseClicked(object? sender, EventArgs e)
    {
        Debug.WriteLine("DEBUG: Przycisk Zapisz i Zamknij kliknięty.");
        await _viewModel.SaveAndCloseBoxAsync();
        
        await Task.Delay(100);
        ScanEntry.Focus();
    }
}