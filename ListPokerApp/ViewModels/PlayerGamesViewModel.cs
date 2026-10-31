using CommunityToolkit.Mvvm.Input;
using ListPokerApp.Services;

namespace ListPokerApp.ViewModels;

public sealed partial class PlayerGamesViewModel(INavigationService navigation) : ViewModelBase, IQueryAttributable
{
    public Guid PlayerId { get; private set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationRoutes.PlayerIdParameter, out var value) &&
            Guid.TryParse(value?.ToString(), out var id))
        {
            PlayerId = id;
            OnPropertyChanged(nameof(PlayerId));
        }
    }

    // Временно, до секции 7: открыть партию из списка.
    [RelayCommand]
    private Task OpenGame() => navigation.GoToGame(Guid.Empty);

    [RelayCommand]
    private Task Back() => navigation.GoBack();
}
