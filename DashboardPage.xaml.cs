namespace MagazynApp;

using MagazynApp.ViewModels;

public partial class DashboardPage : ContentPage
{
    public DashboardPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel; // To łączy przyciski z komendami
    }

    private async void OnStartPickingClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MainPage));
    }

   
}