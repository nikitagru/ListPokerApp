namespace ListPokerApp.Services;

/// <summary>Переходы между экранами. ViewModel не знают о Shell.</summary>
public interface INavigationService
{
    Task GoToStart();
    Task GoToNewGame();
    Task GoToGame(Guid gameId);
    Task GoToPlayers();
    Task GoToPlayerGames(Guid playerId);
    Task GoBack();

#if DEBUG
    Task GoToPrototype();
#endif
}
