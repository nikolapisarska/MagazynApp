using MagazynApp.ViewModels;

namespace MagazynApp;

public partial class DashboardPage : ContentPage
{
    public DashboardPage(MainViewModel viewModel)
    {
        InitializeComponent();
        this.BindingContext = viewModel; // Teraz przyciski EXPORT/IMPORT w końcu zobaczą ViewModel
    }

    private async void OnStartPickingClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MainPage));
    }

    private async void OnVerifyCartonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CartonVerificationPage());
    }
} 