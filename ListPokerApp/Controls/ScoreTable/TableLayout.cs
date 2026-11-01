namespace ListPokerApp.Controls.ScoreTable;

/// <summary>
/// Геометрия таблицы при заданном масштабе: размеры частей, столбцов и строк.
/// Каждая часть (<see cref="TablePart"/>) рисуется в своих координатах с началом в своём левом верхнем углу.
/// </summary>
/// <remarks>
/// Базовые размеры кратны 4, поэтому при любом масштабе из ряда 75–200% с шагом 25% все размеры — целые DIP.
/// Ширины не зависят от текста: длинные имена обрезаются, текст не измеряется.
/// </remarks>
public sealed class TableLayout
{
    public const int MinScalePercent = 75;
    public const int MaxScalePercent = 200;
    public const int ScaleStepPercent = 25;

    /// <summary>Итоговые строки: «Счёт» и «Место».</summary>
    public const int FooterRowCount = 2;

    // Базовые размеры при масштабе 100%, DIP.
    private static readonly float[] BaseServiceWidths = [32, 32, 88]; // №, K, Сдаёт
    private static readonly float[] BasePlayerWidths = [40, 36, 44, 52]; // З, В, Оч, Счёт
    private const float BaseHeaderNameHeight = 28;
    private const float BaseHeaderColumnsHeight = 24;
    private const float BaseRowHeight = 28;
    private const float BaseFooterRowHeight = 28;
    private const float BaseFontSize = 14;
    private const float BaseSmallFontSize = 10;
    private const float BaseCellPadding = 4;

    private readonly float[] _serviceX;
    private readonly float[] _serviceWidths;
    private readonly float[] _playerX;
    private readonly float[] _playerWidths;

    public TableLayout(int playerCount, int rowCount, int scalePercent)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playerCount);
        ArgumentOutOfRangeException.ThrowIfNegative(rowCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(scalePercent, MinScalePercent);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(scalePercent, MaxScalePercent);

        PlayerCount = playerCount;
        RowCount = rowCount;
        ScalePercent = scalePercent;
        Scale = scalePercent / 100f;

        _serviceWidths = Scaled(BaseServiceWidths);
        _serviceX = Offsets(_serviceWidths);
        _playerWidths = Scaled(BasePlayerWidths);
        _playerX = Offsets(_playerWidths);

        SideWidth = _serviceWidths.Sum();
        BlockWidth = _playerWidths.Sum();
        HeaderNameHeight = BaseHeaderNameHeight * Scale;
        HeaderColumnsHeight = BaseHeaderColumnsHeight * Scale;
        RowHeight = BaseRowHeight * Scale;
        FooterRowHeight = BaseFooterRowHeight * Scale;
        FontSize = BaseFontSize * Scale;
        SmallFontSize = BaseSmallFontSize * Scale;
        CellPadding = BaseCellPadding * Scale;
    }

    public int PlayerCount { get; }

    public int RowCount { get; }

    public int ScalePercent { get; }

    /// <summary>Коэффициент масштаба: 0.75–2.</summary>
    public float Scale { get; }

    public float FontSize { get; }

    /// <summary>Мелкий шрифт: номер очереди, отметки.</summary>
    public float SmallFontSize { get; }

    public float CellPadding { get; }

    /// <summary>Ширина служебных столбцов вместе.</summary>
    public float SideWidth { get; }

    /// <summary>Ширина блока одного игрока.</summary>
    public float BlockWidth { get; }

    public float HeaderNameHeight { get; }

    public float HeaderColumnsHeight { get; }

    public float RowHeight { get; }

    public float FooterRowHeight { get; }

    public float HeaderHeight => HeaderNameHeight + HeaderColumnsHeight;

    public float FooterHeight => FooterRowHeight * FooterRowCount;

    public float BodyWidth => BlockWidth * PlayerCount;

    public float BodyHeight => RowHeight * RowCount;

    /// <summary>Размер всей таблицы — для PNG.</summary>
    public SizeF TotalSize => new(SideWidth + BodyWidth, HeaderHeight + BodyHeight + FooterHeight);

    /// <summary>Размер части в её собственных координатах.</summary>
    public SizeF PartSize(TablePart part) => part switch
    {
        TablePart.TopLeft => new(SideWidth, HeaderHeight),
        TablePart.Header => new(BodyWidth, HeaderHeight),
        TablePart.Side => new(SideWidth, BodyHeight),
        TablePart.Body => new(BodyWidth, BodyHeight),
        TablePart.BottomLeft => new(SideWidth, FooterHeight),
        TablePart.Footer => new(BodyWidth, FooterHeight),
        _ => throw new ArgumentOutOfRangeException(nameof(part)),
    };

    /// <summary>Положение левого верхнего угла части во всей таблице — для PNG, где части рисуются без сдвигов.</summary>
    public PointF PartOrigin(TablePart part) => part switch
    {
        TablePart.TopLeft => new(0, 0),
        TablePart.Header => new(SideWidth, 0),
        TablePart.Side => new(0, HeaderHeight),
        TablePart.Body => new(SideWidth, HeaderHeight),
        TablePart.BottomLeft => new(0, HeaderHeight + BodyHeight),
        TablePart.Footer => new(SideWidth, HeaderHeight + BodyHeight),
        _ => throw new ArgumentOutOfRangeException(nameof(part)),
    };

    public float ServiceColumnX(ServiceColumn column) => _serviceX[(int)column];

    public float ServiceColumnWidth(ServiceColumn column) => _serviceWidths[(int)column];

    /// <summary>Левый край блока игрока в координатах шапки, тела и итогов.</summary>
    public float BlockX(int player) => player * BlockWidth;

    public float PlayerColumnX(int player, PlayerColumn column) => BlockX(player) + _playerX[(int)column];

    public float PlayerColumnWidth(PlayerColumn column) => _playerWidths[(int)column];

    /// <summary>Верх строки раздачи в координатах служебных столбцов и тела.</summary>
    public float RowY(int row) => row * RowHeight;

    /// <summary>Строка раздачи целиком в координатах тела — для прокрутки к ней.</summary>
    public RectF RowRect(int row) => new(0, RowY(row), BodyWidth, RowHeight);

    /// <summary>Ячейка игрока в координатах тела.</summary>
    public RectF BodyCell(int row, int player, PlayerColumn column) =>
        new(PlayerColumnX(player, column), RowY(row), PlayerColumnWidth(column), RowHeight);

    /// <summary>Служебная ячейка строки раздачи в координатах служебных столбцов.</summary>
    public RectF SideCell(int row, ServiceColumn column) =>
        new(ServiceColumnX(column), RowY(row), ServiceColumnWidth(column), RowHeight);

    /// <summary>Заголовок служебного столбца в координатах угла сверху (на всю высоту шапки).</summary>
    public RectF CornerCell(ServiceColumn column) =>
        new(ServiceColumnX(column), 0, ServiceColumnWidth(column), HeaderHeight);

    /// <summary>Имя игрока в координатах шапки.</summary>
    public RectF HeaderName(int player) => new(BlockX(player), 0, BlockWidth, HeaderNameHeight);

    /// <summary>Заголовок подстолбца в координатах шапки.</summary>
    public RectF HeaderColumn(int player, PlayerColumn column) =>
        new(PlayerColumnX(player, column), HeaderNameHeight, PlayerColumnWidth(column), HeaderColumnsHeight);

    /// <summary>Значение итоговой строки игрока (на весь блок) в координатах итогов.</summary>
    public RectF FooterCell(int footerRow, int player) =>
        new(BlockX(player), footerRow * FooterRowHeight, BlockWidth, FooterRowHeight);

    /// <summary>Подпись итоговой строки в координатах угла снизу.</summary>
    public RectF FooterLabel(int footerRow) => new(0, footerRow * FooterRowHeight, SideWidth, FooterRowHeight);

    /// <summary>Строки раздач, которые пересекают полосу <paramref name="top"/>…<paramref name="bottom"/>.</summary>
    public (int First, int Last) RowsIn(float top, float bottom)
    {
        if (RowCount == 0 || bottom <= top)
        {
            return (0, -1);
        }

        var first = Math.Clamp((int)(top / RowHeight), 0, RowCount - 1);
        var last = Math.Clamp((int)MathF.Ceiling(bottom / RowHeight) - 1, first, RowCount - 1);
        return (first, last);
    }

    private float[] Scaled(float[] baseSizes) => baseSizes.Select(size => size * Scale).ToArray();

    private static float[] Offsets(float[] sizes)
    {
        var offsets = new float[sizes.Length];
        for (var i = 1; i < sizes.Length; i++)
        {
            offsets[i] = offsets[i - 1] + sizes[i - 1];
        }

        return offsets;
    }
}
