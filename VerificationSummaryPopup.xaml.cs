using CommunityToolkit.Maui.Views;
using MagazynApp.ViewModels;

namespace MagazynApp.Views;

public partial class VerificationSummaryPopup : Popup
{
    public VerificationSummaryPopup(SearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }
}