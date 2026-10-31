namespace ListPokerApp.Services;

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToStart() => Go($"//{NavigationRoutes.Start}");

    public Task GoToNewGame() => Go(NavigationRoutes.NewGame);

    public Task GoToGame(Guid gameId) =>
        Go($"{NavigationRoutes.Game}?{NavigationRoutes.GameIdParameter}={gameId}");

    public Task GoToPlayers() => Go(NavigationRoutes.Players);

    public Task GoToPlayerGames(Guid playerId) =>
        Go($"{NavigationRoutes.PlayerGames}?{NavigationRoutes.PlayerIdParameter}={playerId}");

    public Task GoBack() => Go("..");

    private static Task Go(string route) => Shell.Current.GoToAsync(route);
}
