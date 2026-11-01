using System.Collections.Immutable;
using ListPokerApp.Controls.ScoreTable;

namespace ListPokerApp.Prototype;

internal enum PrototypeStage
{
    Ascent,
    Plateau,
    Descent,
    FinalPlateau,
}

internal sealed record PrototypeCell(int Bid, int Tricks, bool Dark, int Points, int Score);

internal sealed record PrototypeRow(
    int Number,
    int Cards,
    PrototypeStage Stage,
    int Dealer,
    ImmutableArray<PrototypeCell> Cells);

/// <summary>
/// Фиктивная партия для прототипа (секция 1): все строки заполнены случайными числами,
/// одна строка подсвечена как активная.
/// </summary>
internal sealed class PrototypeTable : ITableContent
{
    /// <summary>Как у игры на троих: подъём 1…11, плато 12 ×3, спуск 11…1, финал 12 ×3.</summary>
    public const int RowCountValue = 28;

    private static readonly string[] Names = ["Аня", "Боря", "Вика", "Гоша", "Даша", "Константин Длинный"];

    private PrototypeTable(ImmutableArray<string> players, ImmutableArray<PrototypeRow> rows, int activeRow)
    {
        Players = players;
        Rows = rows;
        ActiveRow = activeRow;
        Totals = rows[^1].Cells.Select(cell => cell.Score).ToImmutableArray();
        Places = Totals.Select(score => 1 + Totals.Count(other => other > score)).ToImmutableArray();
    }

    public ImmutableArray<string> Players { get; }

    public ImmutableArray<PrototypeRow> Rows { get; }

    public ImmutableArray<int> Totals { get; }

    public ImmutableArray<int> Places { get; }

    public int ActiveRow { get; }

    public int PlayerCount => Players.Length;

    public int RowCount => Rows.Length;

    public static PrototypeTable Create(int playerCount, int activeRow, int seed = 42)
    {
        var random = new Random(seed);
        var players = Names.Take(playerCount).ToImmutableArray();
        var scores = new int[playerCount];
        var rows = ImmutableArray.CreateBuilder<PrototypeRow>(RowCountValue);

        foreach (var (cards, stage) in Plan())
        {
            var number = rows.Count + 1;
            var dealer = (number - 1) % playerCount;
            var cells = ImmutableArray.CreateBuilder<PrototypeCell>(playerCount);
            for (var player = 0; player < playerCount; player++)
            {
                var bid = random.Next(0, Math.Min(cards, 4) + 1);
                var tricks = random.NextDouble() < 0.55 ? bid : random.Next(0, Math.Min(cards, 5) + 1);
                var dark = stage == PrototypeStage.FinalPlateau || (player != dealer && random.NextDouble() < 0.12);
                var points = Points(bid, tricks, dark);
                scores[player] += points;
                cells.Add(new PrototypeCell(bid, tricks, dark, points, scores[player]));
            }

            rows.Add(new PrototypeRow(number, cards, stage, dealer, cells.MoveToImmutable()));
        }

        return new PrototypeTable(players, rows.MoveToImmutable(), Math.Clamp(activeRow, 0, RowCountValue - 1));
    }

    public PrototypeTable WithActiveRow(int row) => new(Players, Rows, Math.Clamp(row, 0, Rows.Length - 1));

    public void Draw(ICanvas canvas, TableLayout layout, TablePart part, RectF dirtyRect) =>
        PrototypeRenderer.Draw(canvas, this, layout, part, dirtyRect);

    private static IEnumerable<(int Cards, PrototypeStage Stage)> Plan()
    {
        const int maxCards = 12;
        const int plateau = 3;
        for (var cards = 1; cards < maxCards; cards++)
        {
            yield return (cards, PrototypeStage.Ascent);
        }

        for (var i = 0; i < plateau; i++)
        {
            yield return (maxCards, PrototypeStage.Plateau);
        }

        for (var cards = maxCards - 1; cards >= 1; cards--)
        {
            yield return (cards, PrototypeStage.Descent);
        }

        for (var i = 0; i < plateau; i++)
        {
            yield return (maxCards, PrototypeStage.FinalPlateau);
        }
    }

    private static int Points(int bid, int tricks, bool dark)
    {
        var factor = dark ? 2 : 1;
        if (tricks == bid)
        {
            return (bid == 0 ? 5 : 10 * bid) * factor;
        }

        return tricks > bid ? 2 * tricks * factor : -10 * (bid - tricks) * factor;
    }
}
