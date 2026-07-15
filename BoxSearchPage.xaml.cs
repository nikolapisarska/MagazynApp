using MagazynApp.ViewModels;

namespace MagazynApp;

public partial class BoxSearchPage : ContentPage
{
    private readonly SearchViewModel _viewModel;

    public BoxSearchPage(SearchViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Zostawiamy tylko fokus, nic więcej
        ScanEntry.Focus();
    }
}