using MagazynApp.ViewModels;
using MagazynApp.Model;

namespace MagazynApp;

[QueryProperty(nameof(CurrentBox), "SelectedBox")] 
public partial class BoxSearchPage : ContentPage
{
    public BoxSearchPage(SearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public Box CurrentBox
    {
        set 
        {
            if (BindingContext is SearchViewModel vm)
            {
                vm.CurrentBox = value;
            }
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Zawsze ustawiamy fokus po pojawieniu się strony
        ScanEntry.Focus();
    }

    // Wywołaj to zdarzenie w XAML dla SearchBar: SearchButtonPressed="OnSearchButtonPressed"
    private void OnSearchButtonPressed(object sender, EventArgs e)
    {
        // Po naciśnięciu Enter/zeskanowaniu dajemy chwilę na przetworzenie
        // i przywracamy fokus, aby użytkownik mógł skanować dalej
        MainThread.BeginInvokeOnMainThread(async () => {
            await Task.Delay(200);
            ScanEntry.Focus();
        });
    }
}