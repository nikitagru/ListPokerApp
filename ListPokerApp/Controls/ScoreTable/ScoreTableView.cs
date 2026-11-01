namespace ListPokerApp.Controls.ScoreTable;

/// <summary>
/// Таблица игры из шести частей в сетке 3×2: углы, шапка, служебные столбцы, тело и итоги.
/// </summary>
/// <remarks>
/// Тело лежит в <see cref="ScrollView"/> в полный размер таблицы и прокручивается штатно — колесом, полосами, касанием.
/// Шапка и итоги сдвигаются вслед за ним по X, служебные столбцы — по Y (<c>TranslationX/Y</c>);
/// каждая из этих частей обрезается своей областью. Углы не двигаются.
/// </remarks>
public sealed partial class ScoreTableView : ContentView
{
    public static readonly BindableProperty TableProperty = BindableProperty.Create(
        nameof(Table),
        typeof(ITableContent),
        typeof(ScoreTableView),
        propertyChanged: (view, _, _) => ((ScoreTableView)view).OnTableChanged());

    /// <summary>Масштаб в процентах, 75–200.</summary>
    public static readonly BindableProperty TableScaleProperty = BindableProperty.Create(
        nameof(TableScale),
        typeof(int),
        typeof(ScoreTableView),
        100,
        coerceValue: (_, value) => Math.Clamp((int)value, TableLayout.MinScalePercent, TableLayout.MaxScalePercent),
        propertyChanged: (view, _, _) => ((ScoreTableView)view).OnTableScaleChanged());

    /// <summary>Строка, которая должна оставаться в видимой области: при её смене и после смены масштаба. −1 — нет.</summary>
    public static readonly BindableProperty ActiveRowProperty = BindableProperty.Create(
        nameof(ActiveRow),
        typeof(int),
        typeof(ScoreTableView),
        -1,
        propertyChanged: (view, _, _) => ((ScoreTableView)view).OnActiveRowChanged());

    private readonly Grid _grid;
    private readonly ScrollView _scroll;
    private readonly GraphicsView _header;
    private readonly GraphicsView _side;
    private readonly GraphicsView _body;
    private readonly GraphicsView _footer;
    private readonly GraphicsView[] _parts;

    // Точка тела в центре видимой области (в долях размера тела) до смены масштаба.
    // После перестроения разметки она снова ставится в центр.
    private (double X, double Y)? _zoomAnchor;
    private int _scrollRequest;
    private bool _initialScrollDone;
    private TableHit? _pointerHit;

    public ScoreTableView()
    {
        var topLeft = CreatePart(TablePart.TopLeft);
        _header = CreatePart(TablePart.Header);
        _side = CreatePart(TablePart.Side);
        _body = CreatePart(TablePart.Body);
        var bottomLeft = CreatePart(TablePart.BottomLeft);
        _footer = CreatePart(TablePart.Footer);
        _parts = [topLeft, _header, _side, _body, bottomLeft, _footer]; // в порядке TablePart

        _scroll = new ScrollView { Orientation = ScrollOrientation.Both, Content = _body };
        _scroll.Scrolled += OnScrolled;
        _scroll.SizeChanged += (_, _) => TryInitialScroll();
        _body.SizeChanged += (_, _) => MoveFrozenParts(_scroll.ScrollX, _scroll.ScrollY);

        _grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(0), new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(0), new RowDefinition(GridLength.Star), new RowDefinition(0) },
        };
        _grid.Add(topLeft, 0, 0);
        _grid.Add(ClipArea(_header), 1, 0);
        _grid.Add(ClipArea(_side), 0, 1);
        _grid.Add(_scroll, 1, 1);
        _grid.Add(bottomLeft, 0, 2);
        _grid.Add(ClipArea(_footer), 1, 2);
        Content = _grid;

        // Щелчки и указатель — только по телу: в нём ячейки, которые можно выбрать.
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, e) => RaiseHit(CellTapped, e.GetPosition(_body));
        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += (_, e) => RaiseHit(CellDoubleTapped, e.GetPosition(_body));
        var pointer = new PointerGestureRecognizer();
        pointer.PointerMoved += (_, e) => SetPointerHit(HitAt(e.GetPosition(_body)));
        pointer.PointerExited += (_, _) => SetPointerHit(null);
        _body.GestureRecognizers.Add(tap);
        _body.GestureRecognizers.Add(doubleTap);
        _body.GestureRecognizers.Add(pointer);

        // Платформенные элементы частей существуют только между Loaded и Unloaded.
        Loaded += (_, _) => AttachPlatformInput();
        Unloaded += (_, _) => DetachPlatformInput();
    }

    /// <summary>Прокрутка тела; передаёт событие <see cref="ScrollView.Scrolled"/>.</summary>
    public event EventHandler<ScrolledEventArgs>? Scrolled;

    /// <summary>
    /// Щелчок по ячейке тела. Перед <see cref="CellDoubleTapped"/> может прийти и он (WinUI выдаёт <c>Tapped</c>
    /// на первое нажатие двойного щелчка) — обработка одиночного щелчка не должна мешать двойному.
    /// </summary>
    public event EventHandler<TableHit>? CellTapped;

    public event EventHandler<TableHit>? CellDoubleTapped;

    /// <summary>Под указателем другая ячейка тела; <c>null</c> — указатель ушёл с тела.</summary>
    public event EventHandler<TableHit?>? PointerOverCell;

    public ITableContent? Table
    {
        get => (ITableContent?)GetValue(TableProperty);
        set => SetValue(TableProperty, value);
    }

    public int TableScale
    {
        get => (int)GetValue(TableScaleProperty);
        set => SetValue(TableScaleProperty, value);
    }

    public int ActiveRow
    {
        get => (int)GetValue(ActiveRowProperty);
        set => SetValue(ActiveRowProperty, value);
    }

    public double ScrollX => _scroll.ScrollX;

    public double ScrollY => _scroll.ScrollY;

    /// <summary>Текущая геометрия; <c>null</c>, пока нет <see cref="Table"/>.</summary>
    public TableLayout? CurrentLayout { get; private set; }

    /// <summary>Прокручивает тело так, чтобы строка оказалась в видимой области (с запасом в строку).</summary>
    public Task ScrollToRowAsync(int row, bool animated = false) =>
        ScrollToClampedAsync(_scroll.ScrollX, KeepRowVisible(row, _scroll.ScrollY), animated);

    /// <summary>Прокручивает тело; смещения ограничиваются размером таблицы.</summary>
    public Task ScrollToAsync(double x, double y, bool animated) => ScrollToClampedAsync(x, y, animated);

    /// <summary>
    /// Прямоугольник ячейки в координатах самого контрола — с учётом прокрутки и масштаба.
    /// Для оверлея поверх таблицы (расшифровка «Оч»). <c>null</c>, пока нет таблицы.
    /// </summary>
    public Rect? CellBounds(TableHit hit)
    {
        if (CurrentLayout is not { } layout)
        {
            return null;
        }

        var cell = layout.BodyCell(hit.Row, hit.Player, hit.Column);
        return new Rect(
            layout.SideWidth + cell.X - _scroll.ScrollX,
            layout.HeaderHeight + cell.Y - _scroll.ScrollY,
            cell.Width,
            cell.Height);
    }

    /// <summary>Платформенная обработка колеса мыши; под Windows — в <c>Platforms/Windows/ScoreTableView.Windows.cs</c>.</summary>
    partial void AttachPlatformInput();

    partial void DetachPlatformInput();

    private GraphicsView CreatePart(TablePart part) => new()
    {
        Drawable = new TableDrawable(this, part),
        // У каждой части явный размер (см. RebuildLayout); при выравнивании Fill MAUI такой элемент центрирует,
        // а части должны стоять в левом верхнем углу своей области.
        HorizontalOptions = LayoutOptions.Start,
        VerticalOptions = LayoutOptions.Start,
    };

    // AbsoluteLayout меряет детей без ограничений, поэтому часть получает полный размер, а лишнее обрезается.
    private static AbsoluteLayout ClipArea(View part)
    {
        var clip = new AbsoluteLayout { IsClippedToBounds = true };
        clip.Add(part);
        return clip;
    }

    private void OnTableChanged()
    {
        var hadLayout = CurrentLayout is not null;
        if (Table is { } table && CurrentLayout is { } layout &&
            table.PlayerCount == layout.PlayerCount && table.RowCount == layout.RowCount)
        {
            InvalidateParts();
            return;
        }

        RebuildLayout();
        if (!hadLayout)
        {
            TryInitialScroll();
        }
    }

    private void OnTableScaleChanged()
    {
        // Если прошлое восстановление ещё не применено (быстрые щелчки), центр остаётся прежним.
        if (CurrentLayout is { } old && _zoomAnchor is null && _scroll.Width > 0 && _scroll.Height > 0)
        {
            _zoomAnchor = (
                (_scroll.ScrollX + Math.Min(_scroll.Width, old.BodyWidth) / 2) / old.BodyWidth,
                (_scroll.ScrollY + Math.Min(_scroll.Height, old.BodyHeight) / 2) / old.BodyHeight);
        }

        RebuildLayout();
        _ = ScrollAfterLayoutAsync();
    }

    private void OnActiveRowChanged() => _ = ScrollToRowAsync(ActiveRow, animated: true);

    private void RebuildLayout()
    {
        if (Table is not { } table)
        {
            CurrentLayout = null;
            InvalidateParts();
            return;
        }

        var layout = new TableLayout(table.PlayerCount, table.RowCount, TableScale);
        CurrentLayout = layout;

        _grid.ColumnDefinitions[0].Width = layout.SideWidth;
        _grid.RowDefinitions[0].Height = layout.HeaderHeight;
        _grid.RowDefinitions[2].Height = layout.FooterHeight;
        foreach (var part in Enum.GetValues<TablePart>())
        {
            var size = layout.PartSize(part);
            _parts[(int)part].WidthRequest = size.Width;
            _parts[(int)part].HeightRequest = size.Height;
        }

        InvalidateParts();
    }

    private void InvalidateParts()
    {
        foreach (var part in _parts)
        {
            part.Invalidate();
        }
    }

    private void OnScrolled(object? sender, ScrolledEventArgs e)
    {
        MoveFrozenParts(e.ScrollX, e.ScrollY);
        Scrolled?.Invoke(this, e);
    }

    private void MoveFrozenParts(double x, double y)
    {
        _header.TranslationX = -x;
        _footer.TranslationX = -x;
        _side.TranslationY = -y;
    }

    private TableHit? HitAt(Point? position) =>
        position is { } point && CurrentLayout is { } layout
            ? layout.HitTest(new PointF((float)point.X, (float)point.Y))
            : null;

    private void RaiseHit(EventHandler<TableHit>? handler, Point? position)
    {
        if (HitAt(position) is { } hit)
        {
            handler?.Invoke(this, hit);
        }
    }

    private void SetPointerHit(TableHit? hit)
    {
        if (hit != _pointerHit)
        {
            _pointerHit = hit;
            PointerOverCell?.Invoke(this, hit);
        }
    }

    private void TryInitialScroll()
    {
        if (!_initialScrollDone && CurrentLayout is not null && _scroll.Width > 0 && _scroll.Height > 0)
        {
            _initialScrollDone = true;
            _ = ScrollAfterLayoutAsync();
        }
    }

    /// <summary>После прохода разметки: центр после смены масштаба и активная строка в видимой области.</summary>
    private async Task ScrollAfterLayoutAsync()
    {
        var request = ++_scrollRequest;
        await WhenBodyArrangedAsync();
        if (request != _scrollRequest || CurrentLayout is not { } layout)
        {
            return;
        }

        double x = _scroll.ScrollX;
        double y = _scroll.ScrollY;
        if (_zoomAnchor is { } anchor)
        {
            x = anchor.X * layout.BodyWidth - Math.Min(_scroll.Width, layout.BodyWidth) / 2;
            y = anchor.Y * layout.BodyHeight - Math.Min(_scroll.Height, layout.BodyHeight) / 2;
            _zoomAnchor = null;
        }

        await ScrollToClampedAsync(x, KeepRowVisible(ActiveRow, y), animated: false);
    }

    // Новый размер тела применяется на ближайшем проходе разметки; прокручивать раньше нельзя —
    // ScrollView ограничит смещение прежним размером.
    private async Task WhenBodyArrangedAsync()
    {
        if (CurrentLayout is not { } layout || IsBodyArranged(layout))
        {
            return;
        }

        var arranged = new TaskCompletionSource();
        void OnSizeChanged(object? sender, EventArgs e)
        {
            if (CurrentLayout is { } current && IsBodyArranged(current))
            {
                arranged.TrySetResult();
            }
        }

        _body.SizeChanged += OnSizeChanged;
        try
        {
            await Task.WhenAny(arranged.Task, Task.Delay(500));
        }
        finally
        {
            _body.SizeChanged -= OnSizeChanged;
        }
    }

    private bool IsBodyArranged(TableLayout layout) =>
        Math.Abs(_body.Width - layout.BodyWidth) < 0.5 && Math.Abs(_body.Height - layout.BodyHeight) < 0.5;

    /// <summary>Смещение по Y, при котором строка видна целиком, по возможности с запасом в строку сверху и снизу.</summary>
    private double KeepRowVisible(int row, double y)
    {
        if (CurrentLayout is not { } layout || row < 0 || row >= layout.RowCount || _scroll.Height <= 0)
        {
            return y;
        }

        var viewport = _scroll.Height;
        var margin = Math.Clamp((viewport - layout.RowHeight) / 2, 0, layout.RowHeight);
        var top = layout.RowY(row) - margin;
        var bottom = layout.RowY(row) + layout.RowHeight + margin;
        if (top < y)
        {
            return top;
        }

        return bottom > y + viewport ? bottom - viewport : y;
    }

    private Task ScrollToClampedAsync(double x, double y, bool animated)
    {
        if (CurrentLayout is not { } layout || _scroll.Width <= 0 || _scroll.Height <= 0)
        {
            return Task.CompletedTask;
        }

        x = Math.Clamp(x, 0, Math.Max(0, layout.BodyWidth - _scroll.Width));
        y = Math.Clamp(y, 0, Math.Max(0, layout.BodyHeight - _scroll.Height));

        // Если смещение не меняется, ScrollView может так и не завершить задачу — не вызываем его.
        if (Math.Abs(x - _scroll.ScrollX) < 0.5 && Math.Abs(y - _scroll.ScrollY) < 0.5)
        {
            return Task.CompletedTask;
        }

        return _scroll.ScrollToAsync(x, y, animated);
    }
}
