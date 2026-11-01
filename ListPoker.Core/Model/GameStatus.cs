namespace ListPoker.Core.Model;

/// <summary>Состояние игры. Не хранится — вычисляется из игры (см. <c>GameCalculator</c>).</summary>
public enum GameStatus
{
    InProgress,

    /// <summary>Закрыта последняя раздача.</summary>
    Finished,

    /// <summary>Завершена досрочно, до закрытия последней раздачи.</summary>
    FinishedEarly,
}
