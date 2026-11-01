using System.Collections.Immutable;
using ListPoker.Core.Model;
using ListPoker.Core.Rules;

namespace ListPoker.Core.Calculation;

/// <summary>Всё, что не хранится, а считается из введённых данных (data-storage.md «Вычисляемые значения»).</summary>
public static class GameCalculator
{
    /// <summary>
    /// Один проход по раздачам: очки и накопленный счёт — только у закрытых раздач, итоги и места — по ним же,
    /// активная раздача, состояние игры и ошибки строк.
    /// </summary>
    /// <exception cref="InvalidOperationException">В закрытой раздаче не введён заказ или взятки — данные испорчены.</exception>
    public static GameSummary Calculate(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var playerCount = game.Players.Length;
        var totals = new int[playerCount];
        var notScored = Enumerable.Repeat<int?>(null, playerCount).ToImmutableArray();
        var rounds = ImmutableArray.CreateBuilder<RoundSummary>(game.Rounds.Length);
        var closedCount = 0;
        int? firstOpen = null;

        for (var index = 0; index < game.Rounds.Length; index++)
        {
            var round = game.Rounds[index];
            var errors = RowErrors(index, round);
            if (!round.Closed)
            {
                firstOpen ??= index;
                rounds.Add(new RoundSummary(notScored, notScored, errors));
                continue;
            }

            closedCount++;
            var scores = new int?[playerCount];
            var cumulative = new int?[playerCount];
            for (var player = 0; player < playerCount; player++)
            {
                var entry = round.Entries[player];
                var score = Scoring.Score(
                    entry.Bid ?? throw IncompleteClosedRound(index, player),
                    entry.Tricks ?? throw IncompleteClosedRound(index, player),
                    entry.Dark);
                totals[player] += score;
                scores[player] = score;
                cumulative[player] = totals[player];
            }

            rounds.Add(new RoundSummary([.. scores], [.. cumulative], errors));
        }

        var status = game.FinishedEarly ? GameStatus.FinishedEarly
            : game.Rounds.Length > 0 && game.Rounds[^1].Closed ? GameStatus.Finished
            : GameStatus.InProgress;

        return new GameSummary(
            status,
            status == GameStatus.InProgress ? firstOpen : null,
            closedCount,
            rounds.MoveToImmutable(),
            [.. totals],
            closedCount > 0 ? Placement.Places(totals) : null);
    }

    /// <summary>
    /// Ошибки строки: все заказы введены и их сумма равна K; все взятки введены и их сумма не равна K.
    /// Пока введены не все значения, ошибки нет. Нужна и для проверки черновика правки.
    /// </summary>
    public static ImmutableArray<RowError> RowErrors(int roundIndex, Round round)
    {
        ArgumentNullException.ThrowIfNull(round);

        var errors = ImmutableArray<RowError>.Empty;
        if (SumIfComplete(round, entry => entry.Bid) is { } bidSum && bidSum == round.Cards)
        {
            errors = errors.Add(RowError.BidSumEqualsCards(roundIndex, round));
        }

        if (SumIfComplete(round, entry => entry.Tricks) is { } trickSum && trickSum != round.Cards)
        {
            errors = errors.Add(RowError.TrickSumMismatch(roundIndex, round, trickSum));
        }

        return errors;
    }

    private static int? SumIfComplete(Round round, Func<PlayerRound, int?> value)
    {
        var sum = 0;
        foreach (var entry in round.Entries)
        {
            if (value(entry) is not { } number)
            {
                return null;
            }

            sum += number;
        }

        return sum;
    }

    private static InvalidOperationException IncompleteClosedRound(int round, int player) =>
        new($"Раздача {round + 1} закрыта, но у игрока {player + 1} не введены заказ или взятки.");
}
