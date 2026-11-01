using ListPoker.Core.Session;

namespace ListPokerApp.Services;

/// <summary>
/// Клавиатура и <c>Ctrl</c> + колесо для страницы таблицы. На платформах без клавиатуры событий просто нет —
/// всё доступно указателем.
/// </summary>
public interface IPlatformInput
{
    /// <summary>Нажата клавиша таблицы. Только пока <see cref="IsCapturing"/>.</summary>
    event EventHandler<KeyInput>? KeyPressed;

    /// <summary>
    /// <c>Ctrl</c> + колесо: значение как у колеса — 120 на деление, положительное — от себя (увеличить).
    /// Только пока <see cref="IsCapturing"/>.
    /// </summary>
    event EventHandler<int>? ZoomWheel;

    /// <summary>
    /// Перехват включён: страница таблицы включает его, пока она на экране. Перехваченные клавиши
    /// не доходят до элементов управления — <c>Tab</c> и стрелки не уводят фокус, <c>Пробел</c> и <c>Enter</c> не нажимают кнопки.
    /// </summary>
    bool IsCapturing { get; set; }
}
