using System.Collections.Immutable;

namespace ListPoker.Core.Model;

/// <summary>Раздача.</summary>
/// <param name="Number">Номер, с 1.</param>
/// <param name="Cards">K — карт у каждого игрока, от 1 до M.</param>
/// <param name="Stage">Этап игры.</param>
/// <param name="DealerIndex">Индекс раздающего в <see cref="Game.Players"/>, от 0.</param>
/// <param name="Entries">По одной записи на игрока, в том же порядке, что <see cref="Game.Players"/>.</param>
/// <param name="Closed">Раздача закрыта. Выставляет только сессия ввода, при выполнении условий закрытия.</param>
/// <remarks>
/// <see cref="Number"/>, <see cref="Cards"/>, <see cref="Stage"/>, <see cref="DealerIndex"/> вычисляются при создании игры
/// и не меняются. Равенство — по значению, включая записи игроков (у <see cref="ImmutableArray{T}"/> оно ссылочное).
/// </remarks>
public sealed record Round(
    int Number,
    int Cards,
    RoundStage Stage,
    int DealerIndex,
    ImmutableArray<PlayerRound> Entries,
    bool Closed)
{
    /// <summary>Новая раздача с изменённой записью игрока. Ничего не проверяет.</summary>
    public Round WithEntry(int player, Func<PlayerRound, PlayerRound> change) =>
        this with { Entries = Entries.SetItem(player, change(Entries[player])) };

    public bool Equals(Round? other) =>
        other is not null &&
        Number == other.Number &&
        Cards == other.Cards &&
        Stage == other.Stage &&
        DealerIndex == other.DealerIndex &&
        Closed == other.Closed &&
        Entries.AsSpan().SequenceEqual(other.Entries.AsSpan());

    public override int GetHashCode() =>
        HashCode.Combine(Number, Cards, Stage, DealerIndex, Closed, Entries.IsDefault ? 0 : Entries.Length);
}
