using System.Collections.Immutable;
using ListPoker.Core.Model;
using ListPoker.Core.Rules;

namespace ListPoker.Core.Tests.Rules;

public class BidRulesTests
{
    private static Round Round(
        int players,
        int dealer,
        int cards = 5,
        RoundStage stage = RoundStage.Ascent,
        int[]? dark = null,
        int?[]? bids = null,
        int?[]? tricks = null) =>
        new(
            Number: 1,
            cards,
            stage,
            dealer,
            [.. Enumerable.Range(0, players).Select(player => new PlayerRound(
                bids?[player],
                tricks?[player],
                stage == RoundStage.FinalPlateau || (dark?.Contains(player) ?? false)))],
            Closed: false);

    [Fact]
    public void Without_dark_bids_go_from_left_of_dealer_dealer_last()
    {
        var round = Round(players: 4, dealer: 1);

        BidRules.BidOrder(round).Should().Equal(2, 3, 0, 1);
    }

    [Fact]
    public void Order_wraps_around_table()
    {
        BidRules.BidOrder(Round(players: 4, dealer: 3)).Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void One_dark_player_bids_first()
    {
        var round = Round(players: 4, dealer: 1, dark: [0]);

        BidRules.BidOrder(round).Should().Equal(0, 2, 3, 1);
    }

    [Fact]
    public void Several_dark_players_go_in_table_order_from_left_of_dealer()
    {
        var round = Round(players: 5, dealer: 1, dark: [0, 3]);

        BidRules.BidOrder(round).Should().Equal(3, 0, 2, 4, 1);
    }

    [Fact]
    public void Dark_player_right_after_dealer_is_first()
    {
        var round = Round(players: 5, dealer: 2, dark: [3]);

        BidRules.BidOrder(round).Should().Equal(3, 4, 0, 1, 2);
    }

    [Fact]
    public void Dark_player_right_before_dealer_moves_to_front()
    {
        var round = Round(players: 5, dealer: 2, dark: [1]);

        BidRules.BidOrder(round).Should().Equal(1, 3, 4, 0, 2);
    }

    [Fact]
    public void In_final_everyone_is_dark_and_dealer_is_still_last()
    {
        var round = Round(players: 3, dealer: 0, stage: RoundStage.FinalPlateau);

        BidRules.BidOrder(round).Should().Equal(1, 2, 0);
    }

    [Fact]
    public void Tricks_go_from_left_of_dealer_dealer_last_regardless_of_dark()
    {
        var round = Round(players: 4, dealer: 1, dark: [0]);

        BidRules.TrickOrder(round).Should().Equal(2, 3, 0, 1);
    }

    [Fact]
    public void Dealer_may_not_bid_rest_of_cards()
    {
        var round = Round(players: 3, dealer: 2, cards: 5, bids: [1, 2, null]);

        BidRules.ForbiddenDealerBid(round).Should().Be(2);
    }

    // Крайний случай из gamerules.md §7: остальные заказали ровно K — раздающему нельзя пасовать.
    [Fact]
    public void Dealer_may_not_pass_when_others_bid_all_cards()
    {
        var round = Round(players: 3, dealer: 0, cards: 3, bids: [null, 1, 2]);

        BidRules.ForbiddenDealerBid(round).Should().Be(0);
    }

    [Fact]
    public void Forbidden_can_be_all_cards_when_others_pass()
    {
        var round = Round(players: 3, dealer: 1, cards: 4, bids: [0, null, 0]);

        BidRules.ForbiddenDealerBid(round).Should().Be(4);
    }

    [Fact]
    public void No_restriction_when_others_bid_more_than_cards()
    {
        var round = Round(players: 3, dealer: 2, cards: 3, bids: [2, 2, null]);

        BidRules.ForbiddenDealerBid(round).Should().BeNull();
    }

    [Fact]
    public void Restriction_is_unknown_until_all_others_have_bid()
    {
        var round = Round(players: 4, dealer: 3, cards: 3, bids: [1, null, 0, null]);

        BidRules.ForbiddenDealerBid(round).Should().BeNull();
    }

    [Fact]
    public void Dealers_own_bid_does_not_matter()
    {
        var round = Round(players: 3, dealer: 0, cards: 3, bids: [2, 1, 1]);

        BidRules.ForbiddenDealerBid(round).Should().Be(1);
    }

    [Fact]
    public void Any_player_but_dealer_may_toggle_dark_before_tricks()
    {
        var round = Round(players: 4, dealer: 1, bids: [1, null, 2, null]);

        foreach (var player in new[] { 0, 2, 3 })
        {
            BidRules.CanToggleDark(round, player).IsAllowed.Should().BeTrue();
        }

        BidRules.CanToggleDark(round, 0).Message.Should().BeNull();
    }

    [Fact]
    public void Dealer_may_not_play_dark_outside_final()
    {
        var check = BidRules.CanToggleDark(Round(players: 3, dealer: 1), player: 1);

        check.IsAllowed.Should().BeFalse();
        check.Refusal.Should().Be(DarkToggleRefusal.Dealer);
        check.Message.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Dark_is_locked_in_final_plateau(int player)
    {
        var round = Round(players: 3, dealer: 0, stage: RoundStage.FinalPlateau);

        BidRules.CanToggleDark(round, player).Refusal.Should().Be(DarkToggleRefusal.FinalPlateau);
        BidRules.CanToggleDark(round, player, editing: true).Refusal.Should().Be(DarkToggleRefusal.FinalPlateau);
    }

    [Fact]
    public void Dark_is_locked_once_tricks_are_entered()
    {
        var round = Round(players: 3, dealer: 0, cards: 2, bids: [1, 0, 0], tricks: [null, 1, null]);

        BidRules.CanToggleDark(round, 1).Refusal.Should().Be(DarkToggleRefusal.TricksEntered);
    }

    [Fact]
    public void While_editing_entered_tricks_do_not_lock_dark_but_dealer_rule_stays()
    {
        var round = Round(players: 3, dealer: 0, cards: 2, bids: [1, 0, 0], tricks: [1, 1, 0]) with { Closed = true };

        BidRules.CanToggleDark(round, 1, editing: true).IsAllowed.Should().BeTrue();
        BidRules.CanToggleDark(round, 0, editing: true).Refusal.Should().Be(DarkToggleRefusal.Dealer);
    }

    [Fact]
    public void Every_refusal_has_a_message()
    {
        foreach (var refusal in Enum.GetValues<DarkToggleRefusal>())
        {
            new DarkToggleCheck(refusal).Message.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Orders_contain_every_player_once()
    {
        var round = Round(players: 6, dealer: 4, dark: [5, 0, 2]);

        BidRules.BidOrder(round).Should().BeEquivalentTo(Enumerable.Range(0, 6));
        BidRules.TrickOrder(round).Should().BeEquivalentTo(Enumerable.Range(0, 6));
        BidRules.BidOrder(round).Should().Equal(ImmutableArray.Create(5, 0, 2, 1, 3, 4));
    }
}
