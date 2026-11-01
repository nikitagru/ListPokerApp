using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using MauiScrollView = Microsoft.Maui.Controls.ScrollView;
using MauiView = Microsoft.Maui.Controls.View;

namespace ListPokerApp.Controls.ScoreTable;

/// <summary>
/// Колесо мыши над таблицей под Windows. Штатный ScrollViewer прокручивает только тело и только по вертикали;
/// здесь добавлено: Shift + колесо — по горизонтали, колесо над закреплёнными частями прокручивает тело.
/// </summary>
/// <remarks><c>Ctrl</c> + колесо не трогается — это масштаб (подсекция 1.2).</remarks>
internal sealed class TableWheelScroll
{
    // Одно деление колеса — 120 единиц. Сдвиг на деление — 15% видимой области, как у штатного ScrollViewer
    // (измерено: 36 DIP при высоте 243, 90 DIP при 602), чтобы над любой частью таблицы колесо крутило одинаково.
    private const double NotchDelta = 120;
    private const double ViewportFractionPerNotch = 0.15;

    private readonly ScrollViewer _viewer;
    private readonly List<(UIElement Element, PointerEventHandler Handler)> _subscriptions = [];

    // Куда идёт начатая анимация: следующие деления колеса прибавляются к цели, а не к текущему положению.
    private double? _targetX;
    private double? _targetY;

    private TableWheelScroll(ScrollViewer viewer) => _viewer = viewer;

    /// <summary>Подписывается на колесо тела и закреплённых частей. <c>null</c>, если платформенные элементы ещё не созданы.</summary>
    public static TableWheelScroll? TryAttach(MauiScrollView scroll, MauiView body, IEnumerable<MauiView> frozenParts)
    {
        if (scroll.Handler?.PlatformView is not ScrollViewer viewer || body.Handler?.PlatformView is not UIElement bodyElement)
        {
            return null;
        }

        var frozen = frozenParts.Select(part => part.Handler?.PlatformView as UIElement).ToList();
        if (frozen.Any(element => element is null))
        {
            return null;
        }

        var wheel = new TableWheelScroll(viewer);
        wheel.Subscribe(bodyElement, isBody: true);
        foreach (var element in frozen)
        {
            wheel.Subscribe(element!, isBody: false);
        }

        viewer.ViewChanged += wheel.OnViewChanged;
        return wheel;
    }

    public void Detach()
    {
        foreach (var (element, handler) in _subscriptions)
        {
            element.PointerWheelChanged -= handler;
        }

        _subscriptions.Clear();
        _viewer.ViewChanged -= OnViewChanged;
    }

    private void Subscribe(UIElement element, bool isBody)
    {
        PointerEventHandler handler = (sender, e) => OnWheel((UIElement)sender, e, isBody);
        element.PointerWheelChanged += handler;
        _subscriptions.Add((element, handler));
    }

    // Событие приходит сначала в часть, затем всплывает в ScrollViewer: обработанное здесь он уже не прокручивает.
    private void OnWheel(UIElement sender, PointerRoutedEventArgs e, bool isBody)
    {
        var properties = e.GetCurrentPoint(sender).Properties;
        var delta = properties.MouseWheelDelta;
        var modifiers = e.KeyModifiers;
        if (delta == 0 || modifiers.HasFlag(VirtualKeyModifiers.Control))
        {
            return;
        }

        if (properties.IsHorizontalMouseWheel)
        {
            // Наклон колеса вправо — положительное значение. Над телом ScrollViewer справляется сам.
            if (isBody)
            {
                return;
            }

            ScrollBy(delta, 0);
        }
        else if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
        {
            ScrollBy(-delta, 0);
        }
        else if (isBody)
        {
            return; // обычная вертикальная прокрутка тела — штатная
        }
        else
        {
            ScrollBy(0, -delta);
        }

        e.Handled = true;
    }

    private void ScrollBy(int deltaX, int deltaY)
    {
        double? x = null;
        double? y = null;
        if (deltaX != 0)
        {
            var step = deltaX / NotchDelta * ViewportFractionPerNotch * _viewer.ViewportWidth;
            x = Math.Clamp((_targetX ?? _viewer.HorizontalOffset) + step, 0, _viewer.ScrollableWidth);
            _targetX = x;
        }

        if (deltaY != 0)
        {
            var step = deltaY / NotchDelta * ViewportFractionPerNotch * _viewer.ViewportHeight;
            y = Math.Clamp((_targetY ?? _viewer.VerticalOffset) + step, 0, _viewer.ScrollableHeight);
            _targetY = y;
        }

        _viewer.ChangeView(x, y, null, disableAnimation: false);
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (!e.IsIntermediate)
        {
            _targetX = null;
            _targetY = null;
        }
    }
}
