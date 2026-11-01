namespace ListPokerApp.Controls.ScoreTable;

/// <summary>Отрисовка одной части таблицы для её <see cref="GraphicsView"/>.</summary>
internal sealed class TableDrawable(ScoreTableView owner, TablePart part) : IDrawable
{
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (owner.Table is { } table && owner.CurrentLayout is { } layout)
        {
            table.Draw(canvas, layout, part, dirtyRect);
        }
    }
}
