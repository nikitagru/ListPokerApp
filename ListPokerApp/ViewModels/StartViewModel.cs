using CommunityToolkit.Mvvm.Input;
using ListPokerApp.Services;

namespace ListPokerApp.ViewModels;

public sealed partial class StartViewModel(INavigationService navigation) : ViewModelBase
{
    [RelayCommand]
    private Task NewGame() => navigation.GoToNewGame();

    [RelayCommand]
    private Task Players() => navigation.GoToPlayers();

    // Временно, до секции 5: открыть таблицу без настоящей игры.
    [RelayCommand]
    private Task OpenGame() => navigation.GoToGame(Guid.Empty);
}
