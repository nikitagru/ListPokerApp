using ListPoker.Core.Model;
using ListPoker.Core.Rules;
using Microsoft.Extensions.Time.Testing;

namespace ListPoker.Core.Tests.Rules;

public class GameFactoryTests
{
    private static readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 10, 12, 14, 40, 0, TimeSpan.Zero));

    private static GamePlayer[] Players(int count) =>
        [.. Enumerable.Range(1, count).Select(index => new GamePlayer($"Игрок {index}", Guid.NewGuid()))];

    [Fact]
    public void New_game_has_all_rounds_and_nothing_entered()
    {
        var players = Players(4);

        var game = GameFactory.Create(players, firstDealerIndex: 1, Time);

        game.Players.Should().Equal(players);
        game.FirstDealerIndex.Should().Be(1);
        game.Rounds.Should().Equal(RoundPlanner.BuildRounds(4, 1));
        game.CreatedAt.Should().Be(Time.GetLocalNow());
        game.FinishedAt.Should().BeNull();
        game.FinishedEarly.Should().BeFalse();
    }

    [Fact]
    public void Each_game_gets_its_own_id()
    {
        var players = Players(3);

        var first = GameFactory.Create(players, 0, Time);
        var second = GameFactory.Create(players, 0, Time);

        first.Id.Should().NotBeEmpty();
        first.Id.Should().NotBe(second.Id);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    public void Player_count_outside_3_to_6_is_rejected(int count)
    {
        FluentActions.Invoking(() => GameFactory.Create(Players(count), 0, Time))
            .Should().Throw<ArgumentOutOfRangeException>().WithParameterName("players");
    }

    [Fact]
    public void First_dealer_outside_players_is_rejected()
    {
        FluentActions.Invoking(() => GameFactory.Create(Players(3), 3, Time)).Should().Throw<ArgumentOutOfRangeException>();
    }
}
