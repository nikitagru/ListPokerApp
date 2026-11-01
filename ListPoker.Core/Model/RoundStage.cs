namespace ListPoker.Core.Model;

/// <summary>Этап игры.</summary>
public enum RoundStage
{
    /// <summary>Подъём: K = 1 … M−1.</summary>
    Ascent,

    /// <summary>Первое плато: K = M, N раз.</summary>
    Plateau,

    /// <summary>Спуск: K = M−1 … 1.</summary>
    Descent,

    /// <summary>Финальное плато: K = M, N раз, все играют в тёмную.</summary>
    FinalPlateau,
}
