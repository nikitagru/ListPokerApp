using ListPokerApp.ViewModels;

namespace ListPokerApp.Pages;

public partial class PlayersPage : ContentPage
{
    public PlayersPage(PlayersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
