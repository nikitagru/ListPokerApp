using Microsoft.Maui.Graphics.Skia;

namespace ListPokerApp.Controls.ScoreTable;

/// <summary>
/// Вся таблица одной картинкой PNG через Skia: части рисуются без сдвигов при масштабе 100%
/// тем же рендером, что и на экране.
/// </summary>
/// <remarks>Черновик прототипа (1.3); в 8.2 станет <c>PngExporter</c> над <c>TableState</c>.</remarks>
public static class TablePng
{
    /// <param name="pixelScale">Пикселей на DIP: 2 — чёткий текст при печати и на экранах с высокой плотностью.</param>
    public static void Write(ITableContent table, Stream stream, float pixelScale = 2)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelScale);

        var layout = new TableLayout(table.PlayerCount, table.RowCount, 100);
        var size = layout.TotalSize;
        var width = (int)MathF.Ceiling(size.Width * pixelScale);
        var height = (int)MathF.Ceiling(size.Height * pixelScale);

        using var context = new SkiaBitmapExportContext(width, height, 1);
        var canvas = context.Canvas;
        canvas.Scale(pixelScale, pixelScale);
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(0, 0, size.Width, size.Height);

        foreach (var part in Enum.GetValues<TablePart>())
        {
            var origin = layout.PartOrigin(part);
            var partSize = layout.PartSize(part);
            var area = new RectF(0, 0, partSize.Width, partSize.Height);

            canvas.SaveState();
            canvas.Translate(origin.X, origin.Y);
            canvas.ClipRectangle(area);
            table.Draw(canvas, layout, part, area);
            canvas.RestoreState();
        }

        context.WriteToStream(stream);
    }
}
