using CommunityToolkit.Mvvm.Messaging;
using MagazynApp.ViewModels;

namespace MagazynApp;

public partial class BoxSearchPage
{
    public BoxSearchPage(SearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        WeakReferenceMessenger.Default.Register<FocusScannerMessage>(this, (_, _) =>
        {
            Dispatcher.Dispatch(() => ScanEntry.Focus());
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Wymuszenie skupienia z małym opóźnieniem dla stabilności na desktopie i mobile
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () => ScanEntry.Focus());
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        WeakReferenceMessenger.Default.Unregister<FocusScannerMessage>(this);
    }
}