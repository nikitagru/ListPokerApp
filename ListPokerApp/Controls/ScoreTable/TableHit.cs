namespace ListPokerApp.Controls.ScoreTable;

/// <summary>Зона ячейки под указателем.</summary>
public enum TableHitZone
{
    Cell,

    /// <summary>Значок ● в ячейке «З» — переключение «в тёмную».</summary>
    DarkMark,
}

/// <summary>Ячейка тела под указателем.</summary>
/// <remarks>Форма прототипа (1.2); в секции 6 — <c>CellRef</c> из Core и зоны кнопок ✓/✕ правки.</remarks>
public readonly record struct TableHit(int Row, int Player, PlayerColumn Column, TableHitZone Zone);
