namespace MagazynApp;

public partial class CartonVerificationPage : ContentPage
{
    public CartonVerificationPage(ViewModels.VerificationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // Auto-fokus na pole skanowania po załadowaniu strony
    protected override void OnAppearing()
    {
        base.OnAppearing();
        ScanSearchBar.Focus();
    }
}