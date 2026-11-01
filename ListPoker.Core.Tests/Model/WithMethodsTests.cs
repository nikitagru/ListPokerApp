using ListPoker.Core.Model;

namespace ListPoker.Core.Tests.Model;

public class WithMethodsTests
{
    [Fact]
    public void Game_WithEntry_changes_only_that_entry()
    {
        var game = ModelSamples.Game();

        var changed = game.WithEntry(1, 2, entry => entry with { Bid = 3 });

        changed.Rounds[1].Entries[2].Should().Be(new PlayerRound(3, null, false));
        changed.Rounds[1].Entries.RemoveAt(2).Should().Equal(game.Rounds[1].Entries.RemoveAt(2));
        changed.Rounds[0].Should().BeSameAs(game.Rounds[0]);
        changed.Rounds[2].Should().BeSameAs(game.Rounds[2]);
        changed.Players.Should().Equal(game.Players);
    }

    [Fact]
    public void Game_WithEntry_leaves_original_unchanged()
    {
        var game = ModelSamples.Game();
        var before = ModelSamples.Game();

        _ = game.WithEntry(1, 2, entry => entry with { Bid = 3, Dark = true });

        game.Should().Be(before);
        game.Rounds[1].Entries[2].Should().Be(PlayerRound.Empty);
    }

    [Fact]
    public void Game_WithRound_replaces_only_that_round()
    {
        var game = ModelSamples.Game();

        var changed = game.WithRound(2, round => round with { Closed = true });

        changed.Rounds[2].Closed.Should().BeTrue();
        changed.Rounds[2].Should().Be(game.Rounds[2] with { Closed = true });
        changed.Rounds[0].Should().BeSameAs(game.Rounds[0]);
        changed.Rounds[1].Should().BeSameAs(game.Rounds[1]);
        game.Rounds[2].Closed.Should().BeFalse();
    }

    [Fact]
    public void Round_WithEntry_changes_only_that_entry()
    {
        var round = ModelSamples.Round(number: 4, players: 4);

        var changed = round.WithEntry(0, entry => entry with { Tricks = 2 });

        changed.Entries[0].Should().Be(new PlayerRound(null, 2, false));
        changed.Entries.RemoveAt(0).Should().Equal(round.Entries.RemoveAt(0));
        changed.Should().Be(round with { Entries = changed.Entries });
        round.Entries[0].Should().Be(PlayerRound.Empty);
    }

    [Fact]
    public void Change_function_receives_current_entry()
    {
        var game = ModelSamples.Game().WithEntry(0, 1, entry => entry with { Bid = 2 });

        var changed = game.WithEntry(0, 1, entry => entry with { Tricks = entry.Bid + 1 });

        changed.Rounds[0].Entries[1].Should().Be(new PlayerRound(2, 3, false));
    }
}
