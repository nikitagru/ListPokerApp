using ListPoker.Core.Rules;

namespace ListPoker.Core.Tests.Rules;

public class ScoringTests
{
    // Все примеры из gamerules.md §6.
    [Theory]
    [InlineData(3, 3, false, 30)]
    [InlineData(3, 3, true, 60)]
    [InlineData(2, 4, false, 8)]
    [InlineData(2, 4, true, 16)]
    [InlineData(4, 1, false, -30)]
    [InlineData(4, 1, true, -60)]
    [InlineData(0, 0, false, 5)]
    [InlineData(0, 0, true, 10)]
    public void Examples_from_rules(int bid, int tricks, bool dark, int expected)
    {
        Scoring.Score(bid, tricks, dark).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 2, false, 4)] // перебор при заказе 0: 2 × 2
    [InlineData(0, 2, true, 8)] // тёмный пас с перебором: 2 × 2 × 2
    [InlineData(2, 0, true, -40)] // штраф в тёмную: −10 × 2 × 2
    [InlineData(1, 1, false, 10)]
    [InlineData(12, 12, true, 240)] // наибольшие очки: финал на троих
    [InlineData(12, 0, true, -240)] // наибольший штраф
    public void Edge_cases(int bid, int tricks, bool dark, int expected)
    {
        Scoring.Score(bid, tricks, dark).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0, BidOutcome.Exact)] // выполненный пас — точно
    [InlineData(3, 3, BidOutcome.Exact)]
    [InlineData(2, 4, BidOutcome.Over)]
    [InlineData(0, 1, BidOutcome.Over)]
    [InlineData(4, 1, BidOutcome.Under)]
    public void Outcome_compares_tricks_with_bid(int bid, int tricks, BidOutcome expected)
    {
        Scoring.Outcome(bid, tricks).Should().Be(expected);
    }

    [Theory]
    [InlineData(3, 3, true, "заказ 3, взято 3, в тёмную: 10 × 3 × 2 = +60")] // пример из score-table.md §5
    [InlineData(3, 3, false, "заказ 3, взято 3: 10 × 3 = +30")]
    [InlineData(0, 0, false, "пас, взято 0: +5")]
    [InlineData(0, 0, true, "пас, взято 0, в тёмную: 5 × 2 = +10")]
    [InlineData(2, 4, false, "заказ 2, взято 4, перебор: 2 × 4 = +8")]
    [InlineData(0, 2, true, "пас, взято 2, перебор, в тёмную: 2 × 2 × 2 = +8")]
    [InlineData(4, 1, false, "заказ 4, взято 1, недобор 3: −10 × 3 = −30")]
    [InlineData(4, 1, true, "заказ 4, взято 1, недобор 3, в тёмную: −10 × 3 × 2 = −60")]
    public void Explain_shows_bid_tricks_and_formula(int bid, int tricks, bool dark, string expected)
    {
        Scoring.Explain(bid, tricks, dark).Should().Be(expected);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Negative_values_are_rejected(int bid, int tricks)
    {
        FluentActions.Invoking(() => Scoring.Score(bid, tricks, false)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(30, "+30")]
    [InlineData(-20, "−20")]
    [InlineData(0, "0")]
    public void Signed_points_always_show_sign_with_real_minus(int value, string expected)
    {
        ScoreFormat.Signed(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(214, "214")]
    [InlineData(-5, "−5")]
    [InlineData(0, "0")]
    public void Score_shows_sign_only_when_negative(int value, string expected)
    {
        ScoreFormat.Number(value).Should().Be(expected);
    }
}
