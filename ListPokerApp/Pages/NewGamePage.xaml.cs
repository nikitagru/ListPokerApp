using ListPokerApp.ViewModels;

namespace ListPokerApp.Pages;

public partial class NewGamePage : ContentPage
{
    public NewGamePage(NewGameViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
