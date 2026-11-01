namespace ListPokerApp.Services;

/// <summary>Имена маршрутов Shell и параметров запроса.</summary>
public static class NavigationRoutes
{
    public const string Start = "start";
    public const string NewGame = "new-game";
    public const string Game = "game";
    public const string Players = "players";
    public const string PlayerGames = "player-games";

    /// <summary>Временная страница прототипа (секция 1), только в DEBUG.</summary>
    public const string Prototype = "prototype";

    public const string GameIdParameter = "gameId";
    public const string PlayerIdParameter = "playerId";
}
