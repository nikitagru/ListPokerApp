using System.Globalization;
using ListPoker.Core.Session;
using ListPokerApp.Controls.ScoreTable;
using ListPokerApp.Services;

namespace ListPokerApp.Prototype;

/// <summary>
/// Прототип таблицы (секция 1): пять закреплённых частей и тело в ScrollView на фиктивных данных,
/// масштаб, прокрутка к строке, перехват клавиш, щелчки и указатель с журналом.
/// Логика прямо в коде страницы — страница временная.
/// </summary>
public partial class PrototypePage : ContentPage
{
    private const int JournalLength = 24;
    private const int WheelNotch = 120;

    private readonly INavigationService _navigation;
    private readonly IPlatformInput _input;
    private readonly IDispatcherTimer _statusTimer;
    private readonly LinkedList<string> _journal = new();
    private PrototypeTable _table = PrototypeTable.Create(playerCount: 6, activeRow: 14);
    private int _scrolledEvents;
    private bool _statusDirty = true;
    private int _zoomWheel;

    public PrototypePage(INavigationService navigation, IPlatformInput input)
    {
        InitializeComponent();
        _navigation = navigation;
        _input = input;

        ScoreTable.Table = _table;
        ScoreTable.ActiveRow = _table.ActiveRow;
        ScoreTable.Scrolled += (_, _) =>
        {
            _scrolledEvents++;
            _statusDirty = true;
        };
        ScoreTable.CellTapped += (_, hit) => Log("Щелчок: " + Describe(hit));
        ScoreTable.CellDoubleTapped += (_, hit) => Log("Двойной щелчок: " + Describe(hit));
        ScoreTable.PointerOverCell += (_, hit) => ShowPointer(hit);

        // Строка состояния обновляется по таймеру, а не на каждое событие прокрутки:
        // смена текста вызывает проход разметки и сама может дёргать прокрутку.
        _statusTimer = Dispatcher.CreateTimer();
        _statusTimer.Interval = TimeSpan.FromMilliseconds(250);
        _statusTimer.Tick += (_, _) => UpdateStatus();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _statusTimer.Start();
        _input.KeyPressed += OnKeyPressed;
        _input.ZoomWheel += OnZoomWheel;
        _input.IsCapturing = true;
    }

    protected override void OnDisappearing()
    {
        _input.IsCapturing = false;
        _input.KeyPressed -= OnKeyPressed;
        _input.ZoomWheel -= OnZoomWheel;
        _statusTimer.Stop();
        base.OnDisappearing();
    }

    // Клавиши, которые прототип умеет: масштаб и выбор строки. Остальные только пишутся в журнал.
    private void OnKeyPressed(object? sender, KeyInput key)
    {
        Log("Клавиша " + key);
        switch (key)
        {
            case { Ctrl: true, Key: InputKey.Plus }:
                SetScale(ScoreTable.TableScale + TableLayout.ScaleStepPercent);
                break;
            case { Ctrl: true, Key: InputKey.Minus }:
                SetScale(ScoreTable.TableScale - TableLayout.ScaleStepPercent);
                break;
            case { Ctrl: true, Key: InputKey.Digit0 }:
                SetScale(100);
                break;
            case { Ctrl: false, Key: InputKey.Up }:
                ShowTable(_table.WithActiveRow(_table.ActiveRow - 1));
                break;
            case { Ctrl: false, Key: InputKey.Down }:
                ShowTable(_table.WithActiveRow(_table.ActiveRow + 1));
                break;
        }
    }

    // Сенсорная панель присылает доли деления — копим до целого шага.
    private void OnZoomWheel(object? sender, int delta)
    {
        _zoomWheel += delta;
        var steps = _zoomWheel / WheelNotch;
        _zoomWheel %= WheelNotch;
        if (steps != 0)
        {
            SetScale(ScoreTable.TableScale + steps * TableLayout.ScaleStepPercent);
        }

        Log(string.Create(CultureInfo.InvariantCulture, $"Ctrl+колесо {delta} → {ScoreTable.TableScale}%"));
    }

    private void ShowPointer(TableHit? hit)
    {
        if (hit is not { } cell || ScoreTable.CellBounds(cell) is not { } bounds)
        {
            PointerLabel.Text = "Под указателем: —";
            return;
        }

        PointerLabel.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"Под указателем: {Describe(cell)}\nячейка в контроле: X={bounds.X:0}, Y={bounds.Y:0}, {bounds.Width:0}×{bounds.Height:0}");
    }

    private string Describe(TableHit hit)
    {
        var column = hit.Column switch
        {
            PlayerColumn.Bid => "З",
            PlayerColumn.Tricks => "В",
            PlayerColumn.Points => "Оч",
            _ => "Счёт",
        };
        var mark = hit.Zone == TableHitZone.DarkMark ? " (●)" : string.Empty;
        return $"строка {hit.Row + 1}, {_table.Players[hit.Player]}, {column}{mark}";
    }

    private void Log(string text)
    {
        _journal.AddFirst($"{DateTime.Now:HH:mm:ss.fff}  {text}");
        while (_journal.Count > JournalLength)
        {
            _journal.RemoveLast();
        }

        JournalLabel.Text = string.Join('\n', _journal);
    }

    private void OnBackClicked(object? sender, EventArgs e) => _ = _navigation.GoBack();

    private void OnPlayersClicked(object? sender, EventArgs e)
    {
        if (sender is Button { Text: { } text } && int.TryParse(text, CultureInfo.InvariantCulture, out var count))
        {
            ShowTable(PrototypeTable.Create(count, _table.ActiveRow));
        }
    }

    private void OnRowUpClicked(object? sender, EventArgs e) => ShowTable(_table.WithActiveRow(_table.ActiveRow - 1));

    private void OnRowDownClicked(object? sender, EventArgs e) => ShowTable(_table.WithActiveRow(_table.ActiveRow + 1));

    private void OnZoomOutClicked(object? sender, EventArgs e) => SetScale(ScoreTable.TableScale - TableLayout.ScaleStepPercent);

    private void OnZoomInClicked(object? sender, EventArgs e) => SetScale(ScoreTable.TableScale + TableLayout.ScaleStepPercent);

    private void OnZoomResetClicked(object? sender, EventArgs e) => SetScale(100);

    // Плавная прокрутка до края и обратно — та же анимация, что у колеса; удобно проверять синхронизацию частей.
    private void OnAnimateVerticalClicked(object? sender, EventArgs e) =>
        _ = ScoreTable.ScrollToAsync(ScoreTable.ScrollX, ScoreTable.ScrollY < 1 ? double.MaxValue : 0, animated: true);

    private void OnAnimateHorizontalClicked(object? sender, EventArgs e) =>
        _ = ScoreTable.ScrollToAsync(ScoreTable.ScrollX < 1 ? double.MaxValue : 0, ScoreTable.ScrollY, animated: true);

    // Снимок страницы средствами самого приложения — для проверки отрисовки без снимка экрана.
    private async void OnCaptureClicked(object? sender, EventArgs e)
    {
        try
        {
            var shot = await ((VisualElement)Content).CaptureAsync();
            if (shot is null)
            {
                StatusLabel.Text = "Снимок: не удалось";
                return;
            }

            var path = Path.Combine(Path.GetTempPath(), "ListPokerPrototype", $"shot-{DateTime.Now:HHmmss-fff}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using (var source = await shot.OpenReadAsync())
            await using (var target = File.Create(path))
            {
                await source.CopyToAsync(target);
            }

            StatusLabel.Text = "Снимок: " + path;
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Снимок: " + ex.Message;
        }
    }

    // Вся таблица в PNG через Skia (1.3) — во временную папку; путь и время — в строке состояния.
    private void OnExportPngClicked(object? sender, EventArgs e)
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "ListPokerPrototype", $"table-{DateTime.Now:HHmmss-fff}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            using (var file = File.Create(path))
            {
                TablePng.Write(_table, file);
            }

            StatusLabel.Text = string.Create(CultureInfo.InvariantCulture, $"PNG ({timer.ElapsedMilliseconds} мс): {path}");
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "PNG: " + ex.Message;
        }
    }

    private void ShowTable(PrototypeTable table)
    {
        _table = table;
        ScoreTable.Table = table;
        ScoreTable.ActiveRow = table.ActiveRow;
        _statusDirty = true;
    }

    private void SetScale(int percent)
    {
        ScoreTable.TableScale = percent;
        ZoomButton.Text = $"{ScoreTable.TableScale}%";
        _statusDirty = true;
    }

    private void UpdateStatus()
    {
        if (!_statusDirty)
        {
            return;
        }

        _statusDirty = false;
        StatusLabel.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"Игроков {_table.PlayerCount} · строка {_table.ActiveRow + 1} · масштаб {ScoreTable.TableScale}% · " +
            $"прокрутка X={ScoreTable.ScrollX:0}, Y={ScoreTable.ScrollY:0} · событий Scrolled: {_scrolledEvents}");
    }
}
