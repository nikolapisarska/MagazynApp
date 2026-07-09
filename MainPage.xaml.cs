using MagazynApp.ViewModels;

namespace MagazynApp;


public partial class MainPage 
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
        
        ScanSearchBar.Focus();
    }

    // Obsługa dla SearchBar (zdarzenie SearchButtonPressed wyzwalane przez Enter)
    private void OnScanSearchBarPressed(object sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            if (vm.ProcessScanCommand.CanExecute(null))
            {
                vm.ProcessScanCommand.Execute(null);
            }
        
            // Czyścimy SearchBar
            ScanSearchBar.Text = string.Empty; 
        
            // Wymuszamy fokus
            ScanSearchBar.Focus();
        }
    }

    private async void OnSaveAndCloseClicked(object? sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            await vm.SaveAndCloseBoxAsync();
            ScanSearchBar.Focus();
        }
    }
    
}