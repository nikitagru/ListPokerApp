using System.Collections.Immutable;
using System.Reflection;
using ListPoker.Core.Model;

namespace ListPoker.Core.Tests.Model;

/// <summary>
/// <see cref="Game"/> и <see cref="Round"/> сравниваются по значению, хотя внутри <see cref="ImmutableArray{T}"/>
/// (его собственное равенство — ссылочное). Equals у них написан вручную, поэтому проверяется каждое свойство.
/// </summary>
public class EqualityTests
{
    private static readonly Dictionary<string, Func<Game, Game>> GameChanges = new()
    {
        [nameof(Game.Id)] = game => game with { Id = ModelSamples.Id(999) },
        [nameof(Game.CreatedAt)] = game => game with { CreatedAt = game.CreatedAt.AddMinutes(1) },
        [nameof(Game.FinishedAt)] = game => game with { FinishedAt = game.CreatedAt },
        [nameof(Game.FinishedEarly)] = game => game with { FinishedEarly = true },
        [nameof(Game.Players)] = game => game with { Players = game.Players.SetItem(0, game.Players[0] with { Name = "Другое" }) },
        [nameof(Game.FirstDealerIndex)] = game => game with { FirstDealerIndex = 1 },
        [nameof(Game.Rounds)] = game => game.WithEntry(2, 0, entry => entry with { Dark = true }),
    };

    private static readonly Dictionary<string, Func<Round, Round>> RoundChanges = new()
    {
        [nameof(Round.Number)] = round => round with { Number = 99 },
        [nameof(Round.Cards)] = round => round with { Cards = 99 },
        [nameof(Round.Stage)] = round => round with { Stage = RoundStage.FinalPlateau },
        [nameof(Round.DealerIndex)] = round => round with { DealerIndex = 2 },
        [nameof(Round.Entries)] = round => round.WithEntry(1, entry => entry with { Bid = 0 }),
        [nameof(Round.Closed)] = round => round with { Closed = true },
    };

    [Fact]
    public void Separately_built_equal_games_are_equal()
    {
        var first = ModelSamples.Game().WithEntry(1, 1, entry => entry with { Bid = 1 });
        var second = ModelSamples.Game().WithEntry(1, 1, entry => entry with { Bid = 1 });

        first.Rounds.Equals(second.Rounds).Should().BeFalse("массивы разные, а их собственное равенство ссылочное");
        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Game_differing_in_any_property_is_not_equal()
    {
        var game = ModelSamples.Game();

        foreach (var (property, change) in GameChanges)
        {
            change(game).Should().NotBe(game, "отличается свойство {0}", property);
        }
    }

    [Fact]
    public void Round_differing_in_any_property_is_not_equal()
    {
        var round = ModelSamples.Round(number: 1, players: 3);

        foreach (var (property, change) in RoundChanges)
        {
            change(round).Should().NotBe(round, "отличается свойство {0}", property);
        }
    }

    // Новое свойство без строки в таблицах выше означает, что Equals надо дополнить.
    [Fact]
    public void Changes_cover_every_property()
    {
        PublicProperties<Game>().Should().BeEquivalentTo(GameChanges.Keys);
        PublicProperties<Round>().Should().BeEquivalentTo(RoundChanges.Keys);
    }

    [Fact]
    public void Default_arrays_compare_as_empty()
    {
        var round = new Round(1, 1, RoundStage.Ascent, 0, default, false);
        var empty = round with { Entries = [] };

        round.Equals(empty).Should().BeTrue();
        round.GetHashCode().Should().Be(empty.GetHashCode());
    }

    [Fact]
    public void Default_settings_use_scale_100_and_nothing_else()
    {
        Settings.Default.Should().Be(new Settings(100, null, null));
        Settings.Default.TableScale.Should().BeInRange(Settings.MinScale, Settings.MaxScale);
    }

    private static IEnumerable<string> PublicProperties<T>() =>
        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.Name);
}
