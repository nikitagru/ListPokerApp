using System.Text;

namespace ListPoker.Core.Session;

/// <summary>Клавиша без привязки к платформе. Только те, что нужны таблице (см. score-table.md §5).</summary>
/// <remarks>Цифры основного ряда и цифрового блока дают одни и те же значения.</remarks>
public enum InputKey
{
    Digit0,
    Digit1,
    Digit2,
    Digit3,
    Digit4,
    Digit5,
    Digit6,
    Digit7,
    Digit8,
    Digit9,
    Enter,
    Tab,
    Escape,
    Space,
    Backspace,
    Delete,
    F2,
    Left,
    Up,
    Right,
    Down,

    /// <summary>«+» основного ряда (клавиша «=», с Shift и без) или цифрового блока.</summary>
    Plus,

    /// <summary>«−» основного ряда или цифрового блока.</summary>
    Minus,

    E,
    Y,
    Z,
}

[Flags]
public enum KeyModifiers
{
    None = 0,
    Ctrl = 1,
    Shift = 2,
}

/// <summary>Нажатая клавиша с модификаторами.</summary>
public readonly record struct KeyInput(InputKey Key, KeyModifiers Modifiers = KeyModifiers.None)
{
    public bool Ctrl => Modifiers.HasFlag(KeyModifiers.Ctrl);

    public bool Shift => Modifiers.HasFlag(KeyModifiers.Shift);

    /// <summary>Цифра 0–9 или <c>null</c>, если клавиша не цифровая.</summary>
    public int? Digit => Key is >= InputKey.Digit0 and <= InputKey.Digit9 ? Key - InputKey.Digit0 : null;

    /// <summary>Запись для журнала и отладки: «Ctrl+Shift+Tab», «Ctrl+0».</summary>
    public override string ToString()
    {
        var text = new StringBuilder();
        if (Ctrl)
        {
            text.Append("Ctrl+");
        }

        if (Shift)
        {
            text.Append("Shift+");
        }

        return Digit is { } digit ? text.Append(digit).ToString() : text.Append(Key).ToString();
    }
}
