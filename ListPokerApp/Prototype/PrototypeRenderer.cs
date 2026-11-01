using System.Globalization;
using ListPokerApp.Controls.ScoreTable;
using Font = Microsoft.Maui.Graphics.Font;

namespace ListPokerApp.Prototype;

/// <summary>
/// Черновой рендер прототипа: сетка, текст, фон полос этапов, подсветка активной строки и раздающего.
/// В секции 6 его место займёт <c>TableRenderer</c> с палитрой из <c>TableTheme</c>.
/// </summary>
internal static class PrototypeRenderer
{
    private const string Minus = "−";
    private const string DealerMark = " · сдаёт";

    private static readonly Color Text = Color.FromArgb("#1E2430");
    private static readonly Color MutedText = Color.FromArgb("#6B7585");
    private static readonly Color GridLine = Color.FromArgb("#D3D8E0");
    private static readonly Color BlockLine = Color.FromArgb("#A9B2BF");
    private static readonly Color StageLine = Color.FromArgb("#5F6B7D");
    private static readonly Color HeaderBack = Color.FromArgb("#EBEEF3");
    private static readonly Color DealerHeaderBack = Color.FromArgb("#FFE29A");
    private static readonly Color ActiveRowBack = Color.FromArgb("#FFF0B3");
    private static readonly Color DealerTint = Color.FromRgba(0f, 0f, 0f, 0.06f);
    private static readonly Color LeaderBack = Color.FromArgb("#D9F0DE");
    private static readonly Color Positive = Color.FromArgb("#1B7F3B");
    private static readonly Color Negative = Color.FromArgb("#C62828");

    private static readonly Color[] StageBack =
    [
        Color.FromArgb("#FFFFFF"), // подъём
        Color.FromArgb("#EAF2FC"), // первое плато
        Color.FromArgb("#F7F7EE"), // спуск
        Color.FromArgb("#EFE8FA"), // финальное плато
    ];

    public static void Draw(ICanvas canvas, PrototypeTable table, TableLayout layout, TablePart part, RectF dirtyRect)
    {
        canvas.Antialias = true;
        switch (part)
        {
            case TablePart.TopLeft:
                DrawTopLeft(canvas, layout);
                break;
            case TablePart.Header:
                DrawHeader(canvas, table, layout);
                break;
            case TablePart.Side:
                DrawSide(canvas, table, layout, dirtyRect);
                break;
            case TablePart.Body:
                DrawBody(canvas, table, layout, dirtyRect);
                break;
            case TablePart.BottomLeft:
                DrawBottomLeft(canvas, layout);
                break;
            case TablePart.Footer:
                DrawFooter(canvas, table, layout);
                break;
        }
    }

    private static void DrawTopLeft(ICanvas canvas, TableLayout layout)
    {
        var size = layout.PartSize(TablePart.TopLeft);
        Fill(canvas, HeaderBack, new RectF(0, 0, size.Width, size.Height));

        SetFont(canvas, Font.DefaultBold, layout.FontSize, Text);
        DrawText(canvas, "№", layout.CornerCell(ServiceColumn.Number), 0);
        DrawText(canvas, "K", layout.CornerCell(ServiceColumn.Cards), 0);
        DrawText(canvas, "Сдаёт", layout.CornerCell(ServiceColumn.Dealer), layout.CellPadding, HorizontalAlignment.Left);

        VerticalLine(canvas, GridLine, 1, layout.ServiceColumnX(ServiceColumn.Cards), 0, size.Height);
        VerticalLine(canvas, GridLine, 1, layout.ServiceColumnX(ServiceColumn.Dealer), 0, size.Height);
        HorizontalLine(canvas, BlockLine, 2, 0, size.Width, size.Height - 1);
        VerticalLine(canvas, BlockLine, 2, size.Width - 1, 0, size.Height);
    }

    private static void DrawHeader(ICanvas canvas, PrototypeTable table, TableLayout layout)
    {
        var size = layout.PartSize(TablePart.Header);
        Fill(canvas, HeaderBack, new RectF(0, 0, size.Width, size.Height));
        var dealer = table.Rows[table.ActiveRow].Dealer;

        for (var player = 0; player < table.PlayerCount; player++)
        {
            var name = layout.HeaderName(player);
            if (player == dealer)
            {
                Fill(canvas, DealerHeaderBack, new RectF(name.X, 0, name.Width, size.Height));
            }

            var maxChars = MaxChars(name.Width - 2 * layout.CellPadding, layout.FontSize);
            var title = player == dealer
                ? Truncate(table.Players[player], maxChars - DealerMark.Length) + DealerMark
                : Truncate(table.Players[player], maxChars);
            SetFont(canvas, Font.DefaultBold, layout.FontSize, Text);
            DrawText(canvas, title, name, layout.CellPadding);

            SetFont(canvas, Font.Default, layout.FontSize * 0.9f, MutedText);
            DrawText(canvas, "З", layout.HeaderColumn(player, PlayerColumn.Bid), 0);
            DrawText(canvas, "В", layout.HeaderColumn(player, PlayerColumn.Tricks), 0);
            DrawText(canvas, "Оч", layout.HeaderColumn(player, PlayerColumn.Points), 0);
            DrawText(canvas, "Счёт", layout.HeaderColumn(player, PlayerColumn.Score), 0);

            foreach (var column in (ReadOnlySpan<PlayerColumn>)[PlayerColumn.Tricks, PlayerColumn.Points, PlayerColumn.Score])
            {
                VerticalLine(canvas, GridLine, 1, layout.PlayerColumnX(player, column), layout.HeaderNameHeight, size.Height);
            }

            if (player > 0)
            {
                VerticalLine(canvas, BlockLine, 1, layout.BlockX(player), 0, size.Height);
            }
        }

        HorizontalLine(canvas, GridLine, 1, 0, size.Width, layout.HeaderNameHeight);
        HorizontalLine(canvas, BlockLine, 2, 0, size.Width, size.Height - 1);
    }

    private static void DrawSide(ICanvas canvas, PrototypeTable table, TableLayout layout, RectF dirtyRect)
    {
        var (first, last) = layout.RowsIn(dirtyRect.Top, dirtyRect.Bottom);
        for (var index = first; index <= last; index++)
        {
            var row = table.Rows[index];
            var y = layout.RowY(index);
            Fill(canvas, RowBack(table, index), new RectF(0, y, layout.SideWidth, layout.RowHeight));

            SetFont(canvas, index == table.ActiveRow ? Font.DefaultBold : Font.Default, layout.FontSize, Text);
            var number = row.Number.ToString(CultureInfo.InvariantCulture);
            DrawText(canvas, index == table.ActiveRow ? "▶" + number : number, layout.SideCell(index, ServiceColumn.Number), 0);
            DrawText(canvas, row.Cards.ToString(CultureInfo.InvariantCulture), layout.SideCell(index, ServiceColumn.Cards), 0);

            var dealerCell = layout.SideCell(index, ServiceColumn.Dealer);
            var dealerName = Truncate(table.Players[row.Dealer], MaxChars(dealerCell.Width - 2 * layout.CellPadding, layout.FontSize));
            DrawText(canvas, dealerName, dealerCell, layout.CellPadding, HorizontalAlignment.Left);

            DrawRowLines(canvas, table, layout, index, 0, layout.SideWidth);
        }

        var top = layout.RowY(first);
        var bottom = layout.RowY(last) + layout.RowHeight;
        VerticalLine(canvas, GridLine, 1, layout.ServiceColumnX(ServiceColumn.Cards), top, bottom);
        VerticalLine(canvas, GridLine, 1, layout.ServiceColumnX(ServiceColumn.Dealer), top, bottom);
        VerticalLine(canvas, BlockLine, 2, layout.SideWidth - 1, top, bottom);
    }

    private static void DrawBody(ICanvas canvas, PrototypeTable table, TableLayout layout, RectF dirtyRect)
    {
        var (first, last) = layout.RowsIn(dirtyRect.Top, dirtyRect.Bottom);

        for (var index = first; index <= last; index++)
        {
            var row = table.Rows[index];
            var rowRect = layout.RowRect(index);
            Fill(canvas, RowBack(table, index), rowRect);
            Fill(canvas, DealerTint, new RectF(layout.BlockX(row.Dealer), rowRect.Y, layout.BlockWidth, rowRect.Height));

            for (var player = 0; player < table.PlayerCount; player++)
            {
                var cell = row.Cells[player];

                SetFont(canvas, Font.Default, layout.FontSize, Text);
                var bid = layout.BodyCell(index, player, PlayerColumn.Bid);
                var mark = layout.DarkMarkRect(index, player);
                DrawText(canvas, cell.Bid.ToString(CultureInfo.InvariantCulture), bid with { Width = bid.Width - mark.Width }, 0);
                DrawText(canvas, cell.Tricks.ToString(CultureInfo.InvariantCulture), layout.BodyCell(index, player, PlayerColumn.Tricks), 0);

                canvas.FontColor = cell.Points < 0 ? Negative : cell.Tricks == cell.Bid ? Positive : Text;
                DrawText(canvas, Signed(cell.Points), layout.BodyCell(index, player, PlayerColumn.Points), layout.CellPadding, HorizontalAlignment.Right);

                canvas.FontColor = cell.Score < 0 ? Negative : Text;
                DrawText(canvas, Unsigned(cell.Score), layout.BodyCell(index, player, PlayerColumn.Score), layout.CellPadding, HorizontalAlignment.Right);

                if (cell.Dark)
                {
                    SetFont(canvas, Font.Default, layout.SmallFontSize, Text);
                    DrawText(canvas, "●", mark, 0);
                }
            }

            DrawRowLines(canvas, table, layout, index, 0, layout.BodyWidth);
        }

        var top = layout.RowY(first);
        var bottom = layout.RowY(last) + layout.RowHeight;
        for (var player = 0; player < table.PlayerCount; player++)
        {
            foreach (var column in (ReadOnlySpan<PlayerColumn>)[PlayerColumn.Tricks, PlayerColumn.Points, PlayerColumn.Score])
            {
                VerticalLine(canvas, GridLine, 1, layout.PlayerColumnX(player, column), top, bottom);
            }

            if (player > 0)
            {
                VerticalLine(canvas, BlockLine, 1, layout.BlockX(player), top, bottom);
            }
        }
    }

    private static void DrawBottomLeft(ICanvas canvas, TableLayout layout)
    {
        var size = layout.PartSize(TablePart.BottomLeft);
        Fill(canvas, HeaderBack, new RectF(0, 0, size.Width, size.Height));

        SetFont(canvas, Font.DefaultBold, layout.FontSize, Text);
        DrawText(canvas, "Счёт", layout.FooterLabel(0), layout.CellPadding, HorizontalAlignment.Left);
        DrawText(canvas, "Место", layout.FooterLabel(1), layout.CellPadding, HorizontalAlignment.Left);

        HorizontalLine(canvas, GridLine, 1, 0, size.Width, layout.FooterRowHeight);
        HorizontalLine(canvas, BlockLine, 2, 0, size.Width, 1);
        VerticalLine(canvas, BlockLine, 2, size.Width - 1, 0, size.Height);
    }

    private static void DrawFooter(ICanvas canvas, PrototypeTable table, TableLayout layout)
    {
        var size = layout.PartSize(TablePart.Footer);
        Fill(canvas, HeaderBack, new RectF(0, 0, size.Width, size.Height));

        for (var player = 0; player < table.PlayerCount; player++)
        {
            if (table.Places[player] == 1)
            {
                Fill(canvas, LeaderBack, new RectF(layout.BlockX(player), 0, layout.BlockWidth, size.Height));
            }

            var total = table.Totals[player];
            SetFont(canvas, Font.DefaultBold, layout.FontSize, total < 0 ? Negative : Text);
            DrawText(canvas, Unsigned(total), layout.FooterCell(0, player), layout.CellPadding);

            SetFont(canvas, Font.Default, layout.FontSize, Text);
            DrawText(canvas, table.Places[player].ToString(CultureInfo.InvariantCulture), layout.FooterCell(1, player), layout.CellPadding);

            if (player > 0)
            {
                VerticalLine(canvas, BlockLine, 1, layout.BlockX(player), 0, size.Height);
            }
        }

        HorizontalLine(canvas, GridLine, 1, 0, size.Width, layout.FooterRowHeight);
        HorizontalLine(canvas, BlockLine, 2, 0, size.Width, 1);
    }

    private static Color RowBack(PrototypeTable table, int index) =>
        index == table.ActiveRow ? ActiveRowBack : StageBack[(int)table.Rows[index].Stage];

    /// <summary>Линия под строкой; над первой строкой нового этапа — утолщённая линия.</summary>
    private static void DrawRowLines(ICanvas canvas, PrototypeTable table, TableLayout layout, int index, float left, float right)
    {
        var y = layout.RowY(index);
        HorizontalLine(canvas, GridLine, 1, left, right, y + layout.RowHeight);
        if (index > 0 && table.Rows[index - 1].Stage != table.Rows[index].Stage)
        {
            HorizontalLine(canvas, StageLine, 2, left, right, y);
        }
    }

    private static void SetFont(ICanvas canvas, Font font, float size, Color color)
    {
        canvas.Font = font;
        canvas.FontSize = size;
        canvas.FontColor = color;
    }

    private static void DrawText(ICanvas canvas, string text, RectF rect, float padding, HorizontalAlignment alignment = HorizontalAlignment.Center) =>
        canvas.DrawString(text, rect.X + padding, rect.Y, rect.Width - 2 * padding, rect.Height, alignment, VerticalAlignment.Center);

    private static void Fill(ICanvas canvas, Color color, RectF rect)
    {
        canvas.FillColor = color;
        canvas.FillRectangle(rect);
    }

    private static void HorizontalLine(ICanvas canvas, Color color, float width, float left, float right, float y)
    {
        canvas.StrokeColor = color;
        canvas.StrokeSize = width;
        canvas.DrawLine(left, y, right, y);
    }

    private static void VerticalLine(ICanvas canvas, Color color, float width, float x, float top, float bottom)
    {
        canvas.StrokeColor = color;
        canvas.StrokeSize = width;
        canvas.DrawLine(x, top, x, bottom);
    }

    // Текст не измеряется: число символов оценивается по средней ширине знака.
    private static int MaxChars(float width, float fontSize) => Math.Max(1, (int)(width / (fontSize * 0.6f)));

    private static string Truncate(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..Math.Max(0, maxChars - 1)] + "…";

    private static string Signed(int value) => value switch
    {
        > 0 => "+" + value.ToString(CultureInfo.InvariantCulture),
        < 0 => Minus + (-value).ToString(CultureInfo.InvariantCulture),
        _ => "0",
    };

    private static string Unsigned(int value) =>
        value < 0 ? Minus + (-value).ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
}
