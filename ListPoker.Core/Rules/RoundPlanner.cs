using System.Collections.Immutable;
using ListPoker.Core.Model;

namespace ListPoker.Core.Rules;

/// <summary>Последовательность раздач (gamerules.md §2, data-storage.md §2).</summary>
public static class RoundPlanner
{
    /// <summary>M — максимум карт на руках: <c>36 / N</c> с отбрасыванием остатка.</summary>
    public static int MaxCards(int playerCount)
    {
        EnsurePlayerCount(playerCount);
        return Game.DeckSize / playerCount;
    }

    /// <summary>Всего раздач: <c>2·(M−1) + 2·N</c>.</summary>
    public static int RoundCount(int playerCount) => 2 * (MaxCards(playerCount) - 1) + 2 * playerCount;

    /// <summary>
    /// Все раздачи игры: подъём 1…M−1, первое плато M ×N, спуск M−1…1, финальное плато M ×N.
    /// Номера с 1; раздающий раздачи <c>r</c> — <c>(firstDealer + r − 1) mod N</c>;
    /// в финальном плато у всех «в тёмную», остальные записи пустые.
    /// </summary>
    public static ImmutableArray<Round> BuildRounds(int playerCount, int firstDealerIndex)
    {
        var maxCards = MaxCards(playerCount);
        ArgumentOutOfRangeException.ThrowIfNegative(firstDealerIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(firstDealerIndex, playerCount);

        var rounds = ImmutableArray.CreateBuilder<Round>(RoundCount(playerCount));
        var empty = Enumerable.Repeat(PlayerRound.Empty, playerCount).ToImmutableArray();
        var allDark = Enumerable.Repeat(PlayerRound.Empty with { Dark = true }, playerCount).ToImmutableArray();

        void Add(int cards, RoundStage stage)
        {
            var number = rounds.Count + 1;
            var dealer = (firstDealerIndex + number - 1) % playerCount;
            var entries = stage == RoundStage.FinalPlateau ? allDark : empty;
            rounds.Add(new Round(number, cards, stage, dealer, entries, Closed: false));
        }

        for (var cards = 1; cards < maxCards; cards++)
        {
            Add(cards, RoundStage.Ascent);
        }

        for (var i = 0; i < playerCount; i++)
        {
            Add(maxCards, RoundStage.Plateau);
        }

        for (var cards = maxCards - 1; cards >= 1; cards--)
        {
            Add(cards, RoundStage.Descent);
        }

        for (var i = 0; i < playerCount; i++)
        {
            Add(maxCards, RoundStage.FinalPlateau);
        }

        return rounds.MoveToImmutable();
    }

    internal static void EnsurePlayerCount(int playerCount, string? paramName = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(playerCount, Game.MinPlayers, paramName ?? nameof(playerCount));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(playerCount, Game.MaxPlayers, paramName ?? nameof(playerCount));
    }
}
