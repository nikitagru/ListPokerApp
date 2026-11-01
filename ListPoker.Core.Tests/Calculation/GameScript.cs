using System.Globalization;
using ListPoker.Core.Model;
using ListPoker.Core.Rules;

namespace ListPoker.Core.Tests.Calculation;

/// <summary>
/// Заполнение игры по записи «заказ/взятки» на игрока: «2/2», «3*/2» — в тёмную, «1/» — только заказ, «/» — пусто.
/// </summary>
internal static class GameScript
{
    public static Game NewGame(int players = 3, int firstDealer = 0) => new(
        Guid.NewGuid(),
        new DateTimeOffset(2026, 10, 12, 19, 40, 0, TimeSpan.FromHours(5)),
        FinishedAt: null,
        FinishedEarly: false,
        [.. Enumerable.Range(0, players).Select(index => new GamePlayer(((char)('А' + index)).ToString(), Guid.NewGuid()))],
        firstDealer,
        RoundPlanner.BuildRounds(players, firstDealer));

    /// <summary>Вносит значения в раздачу и, если <paramref name="close"/>, закрывает её. Правила не проверяются.</summary>
    public static Game Enter(this Game game, int roundIndex, bool close, params string[] cells)
    {
        for (var player = 0; player < cells.Length; player++)
        {
            var (bid, dark, tricks) = Parse(cells[player]);
            game = game.WithEntry(roundIndex, player, entry => new PlayerRound(bid, tricks, entry.Dark || dark));
        }

        return close ? game.WithRound(roundIndex, round => round with { Closed = true }) : game;
    }

    /// <summary>Вносит и закрывает первые раздачи по порядку.</summary>
    public static Game Play(this Game game, IReadOnlyList<string[]> rounds)
    {
        for (var index = 0; index < rounds.Count; index++)
        {
            game = game.Enter(index, close: true, rounds[index]);
        }

        return game;
    }

    private static (int? Bid, bool Dark, int? Tricks) Parse(string cell)
    {
        var parts = cell.Split('/');
        var bidText = parts[0];
        var dark = bidText.EndsWith('*');
        bidText = bidText.TrimEnd('*');
        return (
            bidText.Length == 0 ? null : int.Parse(bidText, CultureInfo.InvariantCulture),
            dark,
            parts.Length < 2 || parts[1].Length == 0 ? null : int.Parse(parts[1], CultureInfo.InvariantCulture));
    }
}
