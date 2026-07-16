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

        // Rejestracja wiadomości, która pozwoli ViewModelowi wymusić fokus
        WeakReferenceMessenger.Default.Register<FocusScannerMessage>(this, (r, m) =>
        {
            Dispatcher.Dispatch(() => ScanEntry.Focus());
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Fokus przy wejściu na stronę
        ScanEntry.Focus();
    }
}

// Definicja komunikatu (możesz ją umieścić w nowym pliku lub na końcu tego pliku)
public class FocusScannerMessage { }