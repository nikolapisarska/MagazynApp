using CommunityToolkit.Mvvm.Messaging;
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


        WeakReferenceMessenger.Default.Register<FocusScannerMessage>(this, (r, m) =>
        {
            Dispatcher.Dispatch(() => ScanEntry.Focus());
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

   
        // Wymuszenie skupienia z małym opóźnieniem dla stabilności na desktopie i mobile
        await Task.Delay(250);
        ScanEntry.Focus();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        

        WeakReferenceMessenger.Default.Unregister<FocusScannerMessage>(this);
    }
}