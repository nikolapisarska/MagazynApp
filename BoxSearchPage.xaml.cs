using MagazynApp.ViewModels;
using MagazynApp.Model;

namespace MagazynApp;

[QueryProperty(nameof(CurrentBox), "SelectedBox")] // Nazwa parametru musi być zgodna z tym co wysyłasz
public partial class BoxSearchPage : ContentPage
{
    // Konstruktor strony
    public BoxSearchPage(SearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // Właściwość przekazująca dane do ViewModelu
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
}