using CommunityToolkit.Mvvm.Input;
using ListPokerApp.Services;

namespace ListPokerApp.ViewModels;

public sealed partial class GameViewModel(INavigationService navigation) : ViewModelBase, IQueryAttributable
{
    public Guid GameId { get; private set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationRoutes.GameIdParameter, out var value) &&
            Guid.TryParse(value?.ToString(), out var id))
        {
            GameId = id;
            OnPropertyChanged(nameof(GameId));
        }
    }

    [RelayCommand]
    private Task BackToGames() => navigation.GoToStart();

    [RelayCommand]
    private Task Players() => navigation.GoToPlayers();
}
