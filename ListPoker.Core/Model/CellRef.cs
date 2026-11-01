namespace ListPoker.Core.Model;

/// <summary>Вводимая ячейка игрока в раздаче.</summary>
public enum CellKind
{
    /// <summary>«З» — заказ.</summary>
    Bid,

    /// <summary>«В» — взятки.</summary>
    Tricks,
}

/// <summary>Ссылка на ячейку: фокус ввода, ячейка-виновник ошибки.</summary>
/// <param name="Round">Индекс раздачи, от 0.</param>
/// <param name="Player">Индекс игрока, от 0.</param>
public readonly record struct CellRef(int Round, int Player, CellKind Kind);
