namespace MagazynApp;

using MagazynApp.ViewModels;

public partial class DashboardPage : ContentPage
{
    public DashboardPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel; 
    }

    private async void OnStartPickingClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MainPage));
    }
    private async void OnSearchClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(BoxSearchPage));
    }
}