using ListPoker.Core.Model;

namespace ListPoker.Core.Rules;

/// <summary>Новая игра: все раздачи построены сразу, ничего не введено.</summary>
public static class GameFactory
{
    /// <param name="players">Игроки в порядке рассадки, 3–6.</param>
    /// <param name="firstDealerIndex">Индекс первого раздающего в <paramref name="players"/>.</param>
    /// <param name="time">Источник времени создания (в тестах — подменный).</param>
    public static Game Create(IReadOnlyList<GamePlayer> players, int firstDealerIndex, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(time);
        RoundPlanner.EnsurePlayerCount(players.Count, nameof(players));

        return new Game(
            Guid.NewGuid(),
            time.GetLocalNow(),
            FinishedAt: null,
            FinishedEarly: false,
            [.. players],
            firstDealerIndex,
            RoundPlanner.BuildRounds(players.Count, firstDealerIndex));
    }
}
