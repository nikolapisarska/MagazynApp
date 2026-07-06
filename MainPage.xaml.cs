using MagazynApp.ViewModels;

namespace MagazynApp;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is MainViewModel vm)
        {
            await vm.InitializeLocalDatabaseAsync();
        }

        // Ustawienie fokusa na wejściu
        ScanEntry.Focus();
    }

    // Używamy tylko tej metody. Upewnij się, że w XAML masz: Completed="OnScanEntryCompleted"
    private void OnScanEntryCompleted(object sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            if (vm.ProcessScanCommand.CanExecute(null))
            {
                vm.ProcessScanCommand.Execute(null);
            }
        }
    
        // Przywrócenie fokusa
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () => 
        {
            ScanEntry.Focus();
        });
    }

    private async void OnSaveAndCloseClicked(object? sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            await vm.SaveAndCloseBoxAsync();
            ScanEntry.Focus();
        }
    }
}