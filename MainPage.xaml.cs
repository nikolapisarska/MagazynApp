using MagazynApp.ViewModels;

namespace MagazynApp;

public partial class MainPage : ContentPage // Upewnij się, że dziedziczysz po ContentPage
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
        
        // Zaktualizowano nazwę na ScanEntry
        ScanEntry.Focus();
    }

    // Obsługa zdarzenia Completed dla Entry
    private void OnScanEntryCompleted(object sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            if (vm.ProcessScanCommand.CanExecute(null))
            {
                vm.ProcessScanCommand.Execute(null);
            }
    
            if (sender is Entry entry)
            {
                entry.Text = string.Empty;
                entry.Focus(); 
            }
        }
    }

    private async void OnSaveAndCloseClicked(object? sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            await vm.SaveAndCloseBoxAsync();
            
            // Zaktualizowano nazwę na ScanEntry
            ScanEntry.Focus();
        }
    }
}