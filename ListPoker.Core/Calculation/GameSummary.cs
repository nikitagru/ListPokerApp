using System.Collections.Immutable;
using ListPoker.Core.Model;

namespace ListPoker.Core.Calculation;

/// <summary>Вычисляемое по раздаче: очки, накопленный счёт, ошибки строки.</summary>
/// <param name="Scores">Очки каждого игрока за раздачу; <c>null</c> у незакрытой раздачи.</param>
/// <param name="Cumulative">Накопленный счёт после этой раздачи; <c>null</c> у незакрытой.</param>
/// <param name="Errors">Ошибки строки (может быть и две сразу).</param>
/// <remarks>Равенство массивов — ссылочное: сводки сравниваются поэлементно.</remarks>
public sealed record RoundSummary(
    ImmutableArray<int?> Scores,
    ImmutableArray<int?> Cumulative,
    ImmutableArray<RowError> Errors);

/// <summary>Вычисляемое состояние игры — одна формула для таблицы, списка игр и статистики.</summary>
/// <param name="ActiveRoundIndex">Первая незакрытая раздача; <c>null</c> в завершённой игре (в том числе досрочно).</param>
/// <param name="Rounds">Сводка по каждой раздаче, в порядке <see cref="Game.Rounds"/>.</param>
/// <param name="TotalScores">Счёт игроков по закрытым раздачам.</param>
/// <param name="Places">Места с ничьими (1, 1, 3); <c>null</c>, пока нет ни одной закрытой раздачи.</param>
/// <remarks>Равенство массивов — ссылочное: сводки сравниваются поэлементно.</remarks>
public sealed record GameSummary(
    GameStatus Status,
    int? ActiveRoundIndex,
    int ClosedRoundCount,
    ImmutableArray<RoundSummary> Rounds,
    ImmutableArray<int> TotalScores,
    ImmutableArray<int>? Places)
{
    /// <summary>Индексы игроков на первом месте (при ничьей — несколько); пусто, пока мест нет.</summary>
    public ImmutableArray<int> Winners =>
        Places is { } places ? [.. Enumerable.Range(0, places.Length).Where(player => places[player] == 1)] : [];
}
