using ListPokerApp.ViewModels;

namespace ListPokerApp.Pages;

public partial class GamePage : ContentPage
{
    public GamePage(GameViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
