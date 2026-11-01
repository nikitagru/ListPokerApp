using System.Collections.Immutable;

namespace ListPoker.Core.Model;

/// <summary>Игра — неизменяемый снимок. Изменение даёт новый объект (<see cref="WithRound"/>, <see cref="WithEntry"/>).</summary>
/// <param name="Id">Уникальный идентификатор.</param>
/// <param name="CreatedAt">Дата и время создания.</param>
/// <param name="FinishedAt">Дата и время завершения; <c>null</c>, пока игра идёт.</param>
/// <param name="FinishedEarly">Игра завершена досрочно, до закрытия последней раздачи.</param>
/// <param name="Players">Игроки в порядке рассадки, <see cref="MinPlayers"/>–<see cref="MaxPlayers"/>.</param>
/// <param name="FirstDealerIndex">Индекс первого раздающего в <see cref="Players"/>, от 0.</param>
/// <param name="Rounds">Все раздачи игры, создаются при старте.</param>
/// <remarks>
/// Методы модели ничего не проверяют — проверки в <c>Rules</c> и <c>Session</c>.
/// Равенство — по значению, включая игроков и раздачи (у <see cref="ImmutableArray{T}"/> оно ссылочное).
/// </remarks>
public sealed record Game(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FinishedAt,
    bool FinishedEarly,
    ImmutableArray<GamePlayer> Players,
    int FirstDealerIndex,
    ImmutableArray<Round> Rounds)
{
    public const int MinPlayers = 3;
    public const int MaxPlayers = 6;

    /// <summary>Карт в колоде (от шестёрки до туза).</summary>
    public const int DeckSize = 36;

    /// <summary>Новая игра с изменённой раздачей. <paramref name="index"/> — индекс раздачи, от 0.</summary>
    public Game WithRound(int index, Func<Round, Round> change) =>
        this with { Rounds = Rounds.SetItem(index, change(Rounds[index])) };

    /// <summary>Новая игра с изменённой записью игрока. <paramref name="round"/> — индекс раздачи, от 0.</summary>
    public Game WithEntry(int round, int player, Func<PlayerRound, PlayerRound> change) =>
        WithRound(round, value => value.WithEntry(player, change));

    public bool Equals(Game? other) =>
        other is not null &&
        Id == other.Id &&
        CreatedAt == other.CreatedAt &&
        FinishedAt == other.FinishedAt &&
        FinishedEarly == other.FinishedEarly &&
        FirstDealerIndex == other.FirstDealerIndex &&
        Players.AsSpan().SequenceEqual(other.Players.AsSpan()) &&
        Rounds.AsSpan().SequenceEqual(other.Rounds.AsSpan());

    public override int GetHashCode() =>
        HashCode.Combine(
            Id,
            CreatedAt,
            FinishedAt,
            FinishedEarly,
            FirstDealerIndex,
            Players.IsDefault ? 0 : Players.Length,
            Rounds.IsDefault ? 0 : Rounds.Length);
}
