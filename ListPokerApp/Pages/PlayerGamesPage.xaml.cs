using ListPokerApp.ViewModels;

namespace ListPokerApp.Pages;

public partial class PlayerGamesPage : ContentPage
{
    public PlayerGamesPage(PlayerGamesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
