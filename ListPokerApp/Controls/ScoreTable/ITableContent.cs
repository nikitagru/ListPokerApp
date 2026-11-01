namespace ListPokerApp.Controls.ScoreTable;

/// <summary>
/// Что показывает <see cref="ScoreTableView"/>: размер таблицы и отрисовка её частей.
/// </summary>
/// <remarks>
/// Временный интерфейс прототипа (секция 1). В секции 6 его место займут <c>TableState</c> и <c>TableRenderer</c>.
/// Объект считается неизменяемым: чтобы перерисовать таблицу, в <see cref="ScoreTableView.Table"/> кладётся новый.
/// </remarks>
public interface ITableContent
{
    int PlayerCount { get; }

    int RowCount { get; }

    /// <summary>Рисует часть таблицы в её собственных координатах.</summary>
    void Draw(ICanvas canvas, TableLayout layout, TablePart part, RectF dirtyRect);
}
