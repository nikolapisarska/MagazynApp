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

        // Rejestracja wiadomości wymuszającej fokus z poziomu ViewModelu
        WeakReferenceMessenger.Default.Register<FocusScannerMessage>(this, (r, m) =>
        {
            Dispatcher.Dispatch(() => ScanEntry.Focus());
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Użycie Dispatcher.Dispatch gwarantuje, że kontrolka jest w pełni załadowana
        Dispatcher.Dispatch(() => ScanEntry.Focus());
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Wyrejestrowanie komunikatu przy opuszczaniu strony
        WeakReferenceMessenger.Default.Unregister<FocusScannerMessage>(this);
    }
}