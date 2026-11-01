using System.Globalization;
using System.Text;

namespace ListPoker.Core.Rules;

/// <summary>Исход заказа.</summary>
public enum BidOutcome
{
    /// <summary>Взято ровно столько, сколько заказано (в том числе выполненный пас).</summary>
    Exact,

    /// <summary>Перебор: взято больше заказа.</summary>
    Over,

    /// <summary>Недобор: взято меньше заказа — штраф.</summary>
    Under,
}

/// <summary>Подсчёт очков за раздачу (gamerules.md §6).</summary>
public static class Scoring
{
    public const int PointsPerExactTrick = 10;
    public const int PassPoints = 5;
    public const int PointsPerOverTrick = 2;
    public const int PenaltyPerMissingTrick = 10;
    public const int DarkFactor = 2;

    /// <summary>Очки игрока за раздачу. В тёмную — всё ×2, включая штраф и пас.</summary>
    public static int Score(int bid, int tricks, bool dark)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bid);
        ArgumentOutOfRangeException.ThrowIfNegative(tricks);

        var basePoints = Outcome(bid, tricks) switch
        {
            BidOutcome.Exact => bid == 0 ? PassPoints : PointsPerExactTrick * tricks,
            BidOutcome.Over => PointsPerOverTrick * tricks,
            _ => -PenaltyPerMissingTrick * (bid - tricks),
        };
        return dark ? basePoints * DarkFactor : basePoints;
    }

    public static BidOutcome Outcome(int bid, int tricks) =>
        tricks == bid ? BidOutcome.Exact : tricks > bid ? BidOutcome.Over : BidOutcome.Under;

    /// <summary>
    /// Расшифровка для всплывающей подсказки над «Оч»:
    /// «заказ 3, взято 3, в тёмную: 10 × 3 × 2 = +60», «пас, взято 0: +5»,
    /// «заказ 2, взято 4, перебор: 2 × 4 = +8», «заказ 4, взято 1, недобор 3: −10 × 3 = −30».
    /// </summary>
    public static string Explain(int bid, int tricks, bool dark)
    {
        var score = Score(bid, tricks, dark);
        var outcome = Outcome(bid, tricks);

        var text = new StringBuilder();
        text.Append(bid == 0 ? "пас" : Invariant($"заказ {bid}"));
        text.Append(Invariant($", взято {tricks}"));
        if (outcome == BidOutcome.Over)
        {
            text.Append(", перебор");
        }
        else if (outcome == BidOutcome.Under)
        {
            text.Append(Invariant($", недобор {bid - tricks}"));
        }

        if (dark)
        {
            text.Append(", в тёмную");
        }

        text.Append(": ");

        // Выполненный пас без множителя — просто «+5»: формула из одного числа ничего не объясняет.
        if (outcome == BidOutcome.Exact && bid == 0 && !dark)
        {
            return text.Append(ScoreFormat.Signed(score)).ToString();
        }

        text.Append(outcome switch
        {
            BidOutcome.Exact when bid == 0 => Invariant($"{PassPoints}"),
            BidOutcome.Exact => Invariant($"{PointsPerExactTrick} × {tricks}"),
            BidOutcome.Over => Invariant($"{PointsPerOverTrick} × {tricks}"),
            _ => Invariant($"{ScoreFormat.Minus}{PenaltyPerMissingTrick} × {bid - tricks}"),
        });
        if (dark)
        {
            text.Append(Invariant($" × {DarkFactor}"));
        }

        return text.Append(" = ").Append(ScoreFormat.Signed(score)).ToString();
    }

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
