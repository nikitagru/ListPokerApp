using System.Globalization;
using ListPokerApp.Controls.ScoreTable;
using ListPokerApp.Services;

namespace ListPokerApp.Prototype;

/// <summary>
/// Прототип таблицы (секция 1): пять закреплённых частей и тело в ScrollView на фиктивных данных,
/// масштаб, прокрутка к строке. Логика прямо в коде страницы — страница временная.
/// </summary>
public partial class PrototypePage : ContentPage
{
    private readonly INavigationService _navigation;
    private readonly IDispatcherTimer _statusTimer;
    private PrototypeTable _table = PrototypeTable.Create(playerCount: 6, activeRow: 14);
    private int _scrolledEvents;
    private bool _statusDirty = true;

    public PrototypePage(INavigationService navigation)
    {
        InitializeComponent();
        _navigation = navigation;

        ScoreTable.Table = _table;
        ScoreTable.ActiveRow = _table.ActiveRow;
        ScoreTable.Scrolled += (_, _) =>
        {
            _scrolledEvents++;
            _statusDirty = true;
        };

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
    }

    protected override void OnDisappearing()
    {
        _statusTimer.Stop();
        base.OnDisappearing();
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
