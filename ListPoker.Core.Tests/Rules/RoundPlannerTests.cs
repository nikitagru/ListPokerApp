using ListPoker.Core.Model;
using ListPoker.Core.Rules;

namespace ListPoker.Core.Tests.Rules;

public class RoundPlannerTests
{
    // Последовательности K выписаны вручную по gamerules.md §2, а не той же формулой, что в коде.
    public static TheoryData<int, int[]> CardSequences => new()
    {
        { 3, [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 12, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 12, 12, 12] },
        { 4, [1, 2, 3, 4, 5, 6, 7, 8, 9, 9, 9, 9, 8, 7, 6, 5, 4, 3, 2, 1, 9, 9, 9, 9] },
        { 5, [1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 7, 6, 5, 4, 3, 2, 1, 7, 7, 7, 7, 7] },
        { 6, [1, 2, 3, 4, 5, 6, 6, 6, 6, 6, 6, 5, 4, 3, 2, 1, 6, 6, 6, 6, 6, 6] },
    };

    [Theory]
    [InlineData(3, 12)]
    [InlineData(4, 9)]
    [InlineData(5, 7)]
    [InlineData(6, 6)]
    public void Max_cards_is_deck_divided_by_players(int players, int expected)
    {
        RoundPlanner.MaxCards(players).Should().Be(expected);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(0)]
    public void Player_count_outside_3_to_6_is_rejected(int players)
    {
        FluentActions.Invoking(() => RoundPlanner.MaxCards(players)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => RoundPlanner.BuildRounds(players, 0)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(3, 28)]
    [InlineData(4, 24)]
    [InlineData(5, 22)]
    [InlineData(6, 22)]
    public void Round_count_matches_rules(int players, int expected)
    {
        RoundPlanner.RoundCount(players).Should().Be(expected);
        RoundPlanner.BuildRounds(players, 0).Should().HaveCount(expected);
    }

    [Theory]
    [MemberData(nameof(CardSequences))]
    public void Cards_go_up_plateau_down_final(int players, int[] expected)
    {
        RoundPlanner.BuildRounds(players, 0).Select(round => round.Cards).Should().Equal(expected);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Stages_follow_each_other_with_expected_lengths(int players)
    {
        var maxCards = RoundPlanner.MaxCards(players);
        var stages = RoundPlanner.BuildRounds(players, 0).Select(round => round.Stage).ToList();

        stages.Should().Equal(
            Enumerable.Repeat(RoundStage.Ascent, maxCards - 1)
                .Concat(Enumerable.Repeat(RoundStage.Plateau, players))
                .Concat(Enumerable.Repeat(RoundStage.Descent, maxCards - 1))
                .Concat(Enumerable.Repeat(RoundStage.FinalPlateau, players)));
    }

    [Fact]
    public void One_card_round_occurs_twice_first_and_last_before_final()
    {
        var rounds = RoundPlanner.BuildRounds(3, 0);

        rounds.Where(round => round.Cards == 1).Select(round => round.Number).Should().Equal(1, 25);
    }

    [Fact]
    public void Five_players_use_seven_cards()
    {
        var rounds = RoundPlanner.BuildRounds(5, 0);

        RoundPlanner.MaxCards(5).Should().Be(7);
        rounds.Max(round => round.Cards).Should().Be(7);
        rounds.Should().HaveCount(22);
    }

    [Fact]
    public void Numbers_start_at_one()
    {
        RoundPlanner.BuildRounds(4, 0).Select(round => round.Number).Should().Equal(Enumerable.Range(1, 24));
    }

    [Fact]
    public void Dealer_goes_around_from_first_dealer()
    {
        var dealers = RoundPlanner.BuildRounds(4, firstDealerIndex: 2).Select(round => round.DealerIndex).ToList();

        dealers.Take(6).Should().Equal(2, 3, 0, 1, 2, 3);
        dealers.Should().AllSatisfy(dealer => dealer.Should().BeInRange(0, 3));
        dealers.Select((dealer, index) => (dealer, index)).Should().AllSatisfy(item =>
            item.dealer.Should().Be((2 + item.index) % 4));
    }

    [Fact]
    public void Final_plateau_is_dark_for_everyone_and_other_entries_are_empty()
    {
        var rounds = RoundPlanner.BuildRounds(3, 1);

        foreach (var round in rounds)
        {
            round.Entries.Should().HaveCount(3);
            round.Closed.Should().BeFalse();
            var expected = round.Stage == RoundStage.FinalPlateau ? PlayerRound.Empty with { Dark = true } : PlayerRound.Empty;
            round.Entries.Should().AllBeEquivalentTo(expected, "раздача {0}", round.Number);
        }

        rounds.Count(round => round.Stage == RoundStage.FinalPlateau).Should().Be(3);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void First_dealer_must_be_one_of_the_players(int firstDealer)
    {
        FluentActions.Invoking(() => RoundPlanner.BuildRounds(3, firstDealer)).Should().Throw<ArgumentOutOfRangeException>();
    }
}
