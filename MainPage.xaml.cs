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
        
        await Task.Delay(250);
        ScanEntry.Focus();
    }
}