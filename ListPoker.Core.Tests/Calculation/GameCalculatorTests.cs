using ListPoker.Core.Calculation;
using ListPoker.Core.Model;

namespace ListPoker.Core.Tests.Calculation;

public class GameCalculatorTests
{
    private const int А = 0;
    private const int Б = 1;
    private const int В = 2;

    private static readonly Game FullGame = GameScript.NewGame().Play(ThreePlayerGame.Rounds);

    [Fact]
    public void New_game_has_nothing_scored()
    {
        var summary = GameCalculator.Calculate(GameScript.NewGame());

        summary.Status.Should().Be(GameStatus.InProgress);
        summary.ActiveRoundIndex.Should().Be(0);
        summary.ClosedRoundCount.Should().Be(0);
        summary.TotalScores.Should().Equal(0, 0, 0);
        summary.Places.Should().BeNull();
        summary.Winners.Should().BeEmpty();
        summary.Rounds.Should().HaveCount(28);
        summary.Rounds.Should().AllSatisfy(round =>
        {
            round.Scores.Should().AllSatisfy(score => score.Should().BeNull());
            round.Cumulative.Should().AllSatisfy(score => score.Should().BeNull());
            round.Errors.Should().BeEmpty();
        });
    }

    [Fact]
    public void Full_game_matches_hand_calculated_table()
    {
        var summary = GameCalculator.Calculate(FullGame);

        for (var round = 0; round < 28; round++)
        {
            summary.Rounds[round].Scores.Should().Equal(
                ThreePlayerGame.Scores[round].Select(score => (int?)score), "очки раздачи {0}", round + 1);
            summary.Rounds[round].Cumulative.Should().Equal(
                [ThreePlayerGame.Cumulative[А][round], ThreePlayerGame.Cumulative[Б][round], ThreePlayerGame.Cumulative[В][round]],
                "счёт после раздачи {0}", round + 1);
            summary.Rounds[round].Errors.Should().BeEmpty("раздача {0} корректна", round + 1);
        }

        summary.TotalScores.Should().Equal(ThreePlayerGame.Totals);
    }

    [Fact]
    public void Full_game_is_finished_with_places_and_winner()
    {
        var summary = GameCalculator.Calculate(FullGame);

        summary.Status.Should().Be(GameStatus.Finished);
        summary.ActiveRoundIndex.Should().BeNull();
        summary.ClosedRoundCount.Should().Be(28);
        summary.Places!.Value.Should().Equal(1, 2, 3);
        summary.Winners.Should().Equal(А);
    }

    // Крайний случай из gamerules.md §7: правка старой раздачи пересчитывает все итоги.
    [Fact]
    public void Editing_old_round_shifts_all_later_cumulative_scores()
    {
        // Раздача 2: было А 1/1 (+10), Б 0/0 (+5); стало А 1/0 (−10), Б 0/1 (+2). В без изменений.
        var edited = FullGame.Enter(1, close: true, "1/0", "0/1", "0/1");

        var summary = GameCalculator.Calculate(edited);

        summary.Rounds[0].Cumulative.Should().Equal(5, 2, 5);
        summary.Rounds[1].Scores.Should().Equal(-10, 2, 2);
        for (var round = 1; round < 28; round++)
        {
            summary.Rounds[round].Cumulative.Should().Equal(
                [ThreePlayerGame.Cumulative[А][round] - 20, ThreePlayerGame.Cumulative[Б][round] - 3, ThreePlayerGame.Cumulative[В][round]],
                "счёт после раздачи {0}", round + 1);
        }

        summary.TotalScores.Should().Equal(487, 497, 376);
        summary.Places!.Value.Should().Equal(2, 1, 3);
        summary.Winners.Should().Equal(Б);
    }

    [Fact]
    public void Game_in_progress_points_to_first_open_round()
    {
        var game = GameScript.NewGame().Play(ThreePlayerGame.Rounds[..5]).Enter(5, close: false, "3/", "2/", "/");

        var summary = GameCalculator.Calculate(game);

        summary.Status.Should().Be(GameStatus.InProgress);
        summary.ActiveRoundIndex.Should().Be(5);
        summary.ClosedRoundCount.Should().Be(5);
        summary.TotalScores.Should().Equal(35, 53, 42);
        summary.Places!.Value.Should().Equal(3, 1, 2);
        summary.Rounds[5].Scores.Should().AllSatisfy(score => score.Should().BeNull());
        summary.Rounds[5].Cumulative.Should().AllSatisfy(score => score.Should().BeNull());
    }

    // Сумма взяток не равна K — раздача остаётся открытой, хотя введено всё (score-table.md §4.3).
    [Fact]
    public void Open_round_with_all_values_entered_is_not_scored()
    {
        var game = GameScript.NewGame().Play(ThreePlayerGame.Rounds[..2]).Enter(2, close: false, "1/1", "1/2", "0/1");

        var summary = GameCalculator.Calculate(game);

        summary.ActiveRoundIndex.Should().Be(2);
        summary.ClosedRoundCount.Should().Be(2);
        summary.Rounds[2].Scores.Should().AllSatisfy(score => score.Should().BeNull());
        summary.Rounds[2].Cumulative.Should().AllSatisfy(score => score.Should().BeNull());
        summary.TotalScores.Should().Equal(15, 7, 7);
    }

    [Fact]
    public void Early_finished_game_ignores_values_of_open_round()
    {
        var game = GameScript.NewGame().Play(ThreePlayerGame.Rounds[..3]).Enter(3, close: false, "2/1", "2*/2", "1/") with
        {
            FinishedEarly = true,
            FinishedAt = new DateTimeOffset(2026, 10, 12, 21, 0, 0, TimeSpan.FromHours(5)),
        };

        var summary = GameCalculator.Calculate(game);

        summary.Status.Should().Be(GameStatus.FinishedEarly);
        summary.ActiveRoundIndex.Should().BeNull();
        summary.ClosedRoundCount.Should().Be(3);
        summary.TotalScores.Should().Equal(25, 11, 12);
        summary.Places!.Value.Should().Equal(1, 3, 2);
        summary.Rounds[3].Scores.Should().AllSatisfy(score => score.Should().BeNull());
        game.Rounds[3].Entries[Б].Should().Be(new PlayerRound(2, 2, true), "значения незакрытой раздачи хранятся");
    }

    // Крайний случай из gamerules.md §7.
    [Fact]
    public void Tie_for_first_place_gives_several_winners()
    {
        var game = GameScript.NewGame().Play(ThreePlayerGame.Rounds[..1]);

        var summary = GameCalculator.Calculate(game);

        summary.TotalScores.Should().Equal(5, 2, 5);
        summary.Places!.Value.Should().Equal(1, 3, 1);
        summary.Winners.Should().Equal(А, В);
    }

    // Крайний случай из gamerules.md §7.
    [Fact]
    public void Total_score_may_be_negative()
    {
        var game = GameScript.NewGame().Enter(0, close: true, "1/0", "1/0", "0/1") with { FinishedEarly = true };

        var summary = GameCalculator.Calculate(game);

        summary.TotalScores.Should().Equal(-10, -10, 2);
        summary.Places!.Value.Should().Equal(2, 2, 1);
        summary.Winners.Should().Equal(В);
    }

    [Fact]
    public void Bid_sum_equal_to_cards_is_a_row_error()
    {
        var game = GameScript.NewGame().Play(ThreePlayerGame.Rounds[..2]).Enter(2, close: false, "1/", "1/", "1/");

        var errors = GameCalculator.Calculate(game).Rounds[2].Errors;

        errors.Should().BeEquivalentTo(
        [
            new RowError(
                RowErrorKind.BidSumEqualsCards,
                "Сумма заказов равна K (3)",
                [new CellRef(2, А, CellKind.Bid), new CellRef(2, Б, CellKind.Bid), new CellRef(2, В, CellKind.Bid)]),
        ]);
    }

    [Fact]
    public void Trick_sum_not_equal_to_cards_is_a_row_error()
    {
        var game = GameScript.NewGame().Play(ThreePlayerGame.Rounds[..2]).Enter(2, close: false, "1/1", "1/2", "0/1");

        var errors = GameCalculator.Calculate(game).Rounds[2].Errors;

        errors.Should().BeEquivalentTo(
        [
            new RowError(
                RowErrorKind.TrickSumMismatch,
                "Сумма взяток 4, должно быть 3",
                [new CellRef(2, А, CellKind.Tricks), new CellRef(2, Б, CellKind.Tricks), new CellRef(2, В, CellKind.Tricks)]),
        ]);
    }

    [Fact]
    public void No_error_until_all_values_are_entered()
    {
        var bidsSoFar = GameScript.NewGame().Enter(2, close: false, "1/", "2/", "/");
        var tricksSoFar = GameScript.NewGame().Enter(2, close: false, "1/1", "1/2", "0/");

        GameCalculator.Calculate(bidsSoFar).Rounds[2].Errors.Should().BeEmpty("сумма введённых заказов уже 3, но заказ В не введён");
        GameCalculator.Calculate(tricksSoFar).Rounds[2].Errors.Should().BeEmpty("взятки введены не все");
    }

    [Fact]
    public void Both_errors_can_be_shown_at_once()
    {
        var game = GameScript.NewGame().Enter(2, close: false, "1/2", "1/2", "1/2");

        GameCalculator.Calculate(game).Rounds[2].Errors.Select(error => error.Kind)
            .Should().Equal(RowErrorKind.BidSumEqualsCards, RowErrorKind.TrickSumMismatch);
    }

    [Fact]
    public void Row_errors_can_be_checked_for_a_single_round()
    {
        var round = GameScript.NewGame().Enter(4, close: false, "2/", "1/", "2/").Rounds[4];

        GameCalculator.RowErrors(4, round).Should().ContainSingle()
            .Which.Message.Should().Be("Сумма заказов равна K (5)");
    }

    [Fact]
    public void Closed_round_without_values_is_broken_data()
    {
        var broken = GameScript.NewGame().Enter(0, close: true, "0/0", "0/", "0/1");

        FluentActions.Invoking(() => GameCalculator.Calculate(broken))
            .Should().Throw<InvalidOperationException>().WithMessage("*Раздача 1*игрока 2*");
    }

    [Fact]
    public void Five_player_game_counts_like_any_other()
    {
        var game = GameScript.NewGame(players: 5, firstDealer: 4).Enter(0, close: true, "0/0", "1/1", "0/0", "0/0", "1/0");

        var summary = GameCalculator.Calculate(game);

        summary.Rounds.Should().HaveCount(22);
        summary.TotalScores.Should().Equal(5, 10, 5, 5, -10);
        summary.Places!.Value.Should().Equal(2, 1, 2, 2, 5);
        summary.ActiveRoundIndex.Should().Be(1);
    }
}
