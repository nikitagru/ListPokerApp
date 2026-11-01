using System.Collections.Immutable;
using System.Globalization;
using ListPoker.Core.Model;

namespace ListPoker.Core.Calculation;

public enum RowErrorKind
{
    /// <summary>Все заказы введены, и их сумма равна K.</summary>
    BidSumEqualsCards,

    /// <summary>Все взятки введены, и их сумма не равна K.</summary>
    TrickSumMismatch,
}

/// <summary>Ошибка строки раздачи: текст для строки подсказки и ячейки, которые получают красную рамку.</summary>
/// <remarks>Равенство <see cref="Culprits"/> — ссылочное, как у любого <see cref="ImmutableArray{T}"/>.</remarks>
public sealed record RowError(RowErrorKind Kind, string Message, ImmutableArray<CellRef> Culprits)
{
    /// <summary>«Сумма заказов равна K (3)» — виновники все заказы строки.</summary>
    public static RowError BidSumEqualsCards(int roundIndex, Round round) => new(
        RowErrorKind.BidSumEqualsCards,
        string.Create(CultureInfo.InvariantCulture, $"Сумма заказов равна K ({round.Cards})"),
        Cells(roundIndex, round, CellKind.Bid));

    /// <summary>«Сумма взяток 4, должно быть 3» — виновники все взятки строки.</summary>
    public static RowError TrickSumMismatch(int roundIndex, Round round, int trickSum) => new(
        RowErrorKind.TrickSumMismatch,
        string.Create(CultureInfo.InvariantCulture, $"Сумма взяток {trickSum}, должно быть {round.Cards}"),
        Cells(roundIndex, round, CellKind.Tricks));

    private static ImmutableArray<CellRef> Cells(int roundIndex, Round round, CellKind kind) =>
        [.. Enumerable.Range(0, round.Entries.Length).Select(player => new CellRef(roundIndex, player, kind))];
}
