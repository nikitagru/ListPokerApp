using System.Globalization;

namespace ListPoker.Core.Rules;

/// <summary>
/// Запись очков и счёта для показа: минус — знак «−» (U+2212), как в спецификации.
/// Одно место для таблицы, подсказок, расшифровки и итогов.
/// </summary>
/// <remarks>Для CSV не подходит: Excel не считает «−20» числом (см. 8.1).</remarks>
public static class ScoreFormat
{
    public const char Minus = '−';

    /// <summary>Очки за раздачу — со знаком всегда: «+30», «−20», «0».</summary>
    public static string Signed(int value) => value switch
    {
        > 0 => "+" + Digits(value),
        < 0 => Minus + Digits(-value),
        _ => "0",
    };

    /// <summary>Счёт — знак только у отрицательного: «214», «−5».</summary>
    public static string Number(int value) => value < 0 ? Minus + Digits(-value) : Digits(value);

    private static string Digits(int value) => value.ToString(CultureInfo.InvariantCulture);
}
