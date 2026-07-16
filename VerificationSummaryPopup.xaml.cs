using CommunityToolkit.Maui.Views;
using MagazynApp.Model;

namespace MagazynApp.Views;

public partial class VerificationSummaryPopup : Popup
{
    public VerificationSummaryPopup(Box box)
    {
        InitializeComponent();
        BindingContext = box;
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }
}