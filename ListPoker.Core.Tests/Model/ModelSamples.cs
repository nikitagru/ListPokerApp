using System.Collections.Immutable;
using ListPoker.Core.Model;

namespace ListPoker.Core.Tests.Model;

/// <summary>Небольшие игры для тестов модели. Раздачи построены вручную, без правил игры.</summary>
internal static class ModelSamples
{
    public static readonly DateTimeOffset CreatedAt = new(2026, 10, 12, 19, 40, 0, TimeSpan.FromHours(5));

    public static Game Game(int players = 3, int rounds = 3) => new(
        Id(100),
        CreatedAt,
        FinishedAt: null,
        FinishedEarly: false,
        Enumerable.Range(0, players).Select(index => new GamePlayer($"Игрок {index + 1}", Id(index + 1))).ToImmutableArray(),
        FirstDealerIndex: 0,
        Enumerable.Range(0, rounds).Select(index => Round(index + 1, players)).ToImmutableArray());

    public static Round Round(int number, int players) => new(
        number,
        Cards: number,
        RoundStage.Ascent,
        DealerIndex: (number - 1) % players,
        Enumerable.Repeat(PlayerRound.Empty, players).ToImmutableArray(),
        Closed: false);

    public static Guid Id(int value) => new(value, 0, 0, new byte[8]);
}
