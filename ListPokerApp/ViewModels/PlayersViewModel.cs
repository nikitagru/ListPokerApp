using CommunityToolkit.Mvvm.Input;
using ListPokerApp.Services;

namespace ListPokerApp.ViewModels;

public sealed partial class PlayersViewModel(INavigationService navigation) : ViewModelBase
{
    // Временно, до секции 7: открыть список игр несуществующего игрока.
    [RelayCommand]
    private Task OpenPlayer() => navigation.GoToPlayerGames(Guid.Empty);

    [RelayCommand]
    private Task Back() => navigation.GoBack();
}
