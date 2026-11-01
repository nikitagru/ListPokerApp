using System.Collections.Immutable;
using ListPoker.Core.Model;

namespace ListPoker.Core.Rules;

/// <summary>Почему нельзя поставить или снять «в тёмную».</summary>
public enum DarkToggleRefusal
{
    /// <summary>Финальное плато: «в тёмную» у всех и не снимается.</summary>
    FinalPlateau,

    /// <summary>Раздающему «в тёмную» недоступно (вне финала).</summary>
    Dealer,

    /// <summary>В активной раздаче уже вводятся взятки.</summary>
    TricksEntered,
}

/// <summary>Результат проверки «в тёмную»: разрешено или причина отказа с текстом для подсказки.</summary>
public readonly record struct DarkToggleCheck(DarkToggleRefusal? Refusal)
{
    public static DarkToggleCheck Allowed => default;

    public bool IsAllowed => Refusal is null;

    public string? Message => Refusal switch
    {
        DarkToggleRefusal.FinalPlateau => "В финальных раздачах все играют в тёмную",
        DarkToggleRefusal.Dealer => "Раздающему «в тёмную» недоступно",
        DarkToggleRefusal.TricksEntered => "Взятки уже вводятся: «в тёмную» больше не меняется",
        _ => null,
    };
}

/// <summary>Порядок заказов и взяток, запрещённый заказ раздающего, «в тёмную» (gamerules.md §3–5).</summary>
/// <remarks>Игрок с индексом <c>i + 1</c> сидит слева от игрока <c>i</c>; число игроков — длина записей раздачи.</remarks>
public static class BidRules
{
    /// <summary>
    /// Порядок заказов: сначала «тёмные» по кругу от игрока слева от раздающего, затем остальные по кругу,
    /// раздающий всегда последний (в том числе в финале, где «тёмные» все).
    /// </summary>
    public static ImmutableArray<int> BidOrder(Round round)
    {
        var others = AroundTable(round).SkipLast(1).ToList();
        return
        [
            .. others.Where(player => round.Entries[player].Dark),
            .. others.Where(player => !round.Entries[player].Dark),
            round.DealerIndex,
        ];
    }

    /// <summary>Порядок взяток: по кругу от игрока слева от раздающего, раздающий последний.</summary>
    public static ImmutableArray<int> TrickOrder(Round round) => [.. AroundTable(round)];

    /// <summary>
    /// Запрещённый заказ раздающего: <c>K − Σ заказов остальных</c>, когда все остальные заказы введены
    /// и число лежит в 0…K; иначе <c>null</c> (запрета нет или он ещё не известен).
    /// </summary>
    public static int? ForbiddenDealerBid(Round round)
    {
        var othersSum = 0;
        for (var player = 0; player < round.Entries.Length; player++)
        {
            if (player == round.DealerIndex)
            {
                continue;
            }

            if (round.Entries[player].Bid is not { } bid)
            {
                return null;
            }

            othersSum += bid;
        }

        var forbidden = round.Cards - othersSum;
        return forbidden >= 0 ? forbidden : null;
    }

    /// <summary>Можно ли поставить или снять «в тёмную» у игрока.</summary>
    /// <param name="editing">
    /// Раздача на правке: взятки в ней уже введены, а отметку менять можно (score-table.md §7).
    /// Запреты для раздающего и финала действуют и при правке.
    /// </param>
    public static DarkToggleCheck CanToggleDark(Round round, int player, bool editing = false)
    {
        if (round.Stage == RoundStage.FinalPlateau)
        {
            return new DarkToggleCheck(DarkToggleRefusal.FinalPlateau);
        }

        if (player == round.DealerIndex)
        {
            return new DarkToggleCheck(DarkToggleRefusal.Dealer);
        }

        if (!editing && round.Entries.Any(entry => entry.Tricks is not null))
        {
            return new DarkToggleCheck(DarkToggleRefusal.TricksEntered);
        }

        return DarkToggleCheck.Allowed;
    }

    // Игроки по кругу, начиная с сидящего слева от раздающего; раздающий последний.
    private static IEnumerable<int> AroundTable(Round round)
    {
        var count = round.Entries.Length;
        for (var step = 1; step <= count; step++)
        {
            yield return (round.DealerIndex + step) % count;
        }
    }
}
