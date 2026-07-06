using MagazynApp.ViewModels;

namespace MagazynApp;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ScanEntry.Focus();

        if (BindingContext is MainViewModel vm)
        {
            // Metoda jest wywoływana z ViewModelu, a nie definiowana tutaj
            await vm.InitializeLocalDatabaseAsync();
        }
    }

    private void OnScanEntryCompleted(object? sender, EventArgs e)
    {
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(80), () =>
        {
            ScanEntry.Focus();
        });
    }

    private async void OnSaveAndCloseClicked(object? sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            await vm.SaveAndCloseBoxAsync();
            ScanEntry.Focus();
        }
    }
}