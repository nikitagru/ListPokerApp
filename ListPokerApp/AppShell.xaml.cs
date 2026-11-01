using ListPokerApp.Pages;
using ListPokerApp.Services;

namespace ListPokerApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(NavigationRoutes.NewGame, typeof(NewGamePage));
        Routing.RegisterRoute(NavigationRoutes.Game, typeof(GamePage));
        Routing.RegisterRoute(NavigationRoutes.Players, typeof(PlayersPage));
        Routing.RegisterRoute(NavigationRoutes.PlayerGames, typeof(PlayerGamesPage));
#if DEBUG
        Routing.RegisterRoute(NavigationRoutes.Prototype, typeof(Prototype.PrototypePage));
#endif
    }
}
