using ListPoker.Core.Session;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
using MauiApplication = Microsoft.Maui.Controls.Application;
using WinWindow = Microsoft.UI.Xaml.Window;

namespace ListPokerApp.Services;

/// <summary>
/// Клавиатура и <c>Ctrl</c> + колесо под Windows: <c>PreviewKeyDown</c> и <c>PointerWheelChanged</c>
/// корневого элемента окна WinUI.
/// </summary>
/// <remarks>
/// <c>PreviewKeyDown</c> идёт от корня к элементу в фокусе раньше, чем WinUI переводит фокус по <c>Tab</c> и стрелкам
/// или нажимает кнопку <c>Пробелом</c>; помеченная <c>Handled</c> клавиша дальше не идёт.
/// Диалоги и всплывающие окна лежат вне содержимого окна, поэтому их клавиши сюда не попадают и работают как обычно.
/// Клавиши с <c>Alt</c> не перехватываются — это клавиши системы.
/// </remarks>
public sealed class WindowsPlatformInput : IPlatformInput
{
    private const VirtualKey OemPlus = (VirtualKey)0xBB;  // «=» / «+» основного ряда
    private const VirtualKey OemMinus = (VirtualKey)0xBD; // «-» основного ряда

    private readonly PointerEventHandler _wheelHandler;
    private UIElement? _root;
    private bool _isCapturing;

    public WindowsPlatformInput() => _wheelHandler = OnPointerWheelChanged;

    public event EventHandler<KeyInput>? KeyPressed;

    public event EventHandler<int>? ZoomWheel;

    public bool IsCapturing
    {
        get => _isCapturing;
        set
        {
            _isCapturing = value;
            if (value)
            {
                EnsureAttached();
            }
        }
    }

    /// <summary>Перевод клавиши WinUI; <c>null</c> — клавиша таблице не нужна и не перехватывается.</summary>
    internal static InputKey? Translate(VirtualKey key) => key switch
    {
        >= VirtualKey.Number0 and <= VirtualKey.Number9 => InputKey.Digit0 + (key - VirtualKey.Number0),
        >= VirtualKey.NumberPad0 and <= VirtualKey.NumberPad9 => InputKey.Digit0 + (key - VirtualKey.NumberPad0),
        VirtualKey.Enter => InputKey.Enter,
        VirtualKey.Tab => InputKey.Tab,
        VirtualKey.Escape => InputKey.Escape,
        VirtualKey.Space => InputKey.Space,
        VirtualKey.Back => InputKey.Backspace,
        VirtualKey.Delete => InputKey.Delete,
        VirtualKey.F2 => InputKey.F2,
        VirtualKey.Left => InputKey.Left,
        VirtualKey.Up => InputKey.Up,
        VirtualKey.Right => InputKey.Right,
        VirtualKey.Down => InputKey.Down,
        VirtualKey.Add or OemPlus => InputKey.Plus,
        VirtualKey.Subtract or OemMinus => InputKey.Minus,
        VirtualKey.E => InputKey.E,
        VirtualKey.Y => InputKey.Y,
        VirtualKey.Z => InputKey.Z,
        _ => null,
    };

    // Корень окна появляется после создания окна MAUI; подписываемся при первом включении перехвата.
    private void EnsureAttached()
    {
        var window = MauiApplication.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as WinWindow;
        if (window?.Content is not { } root || ReferenceEquals(root, _root))
        {
            return;
        }

        if (_root is not null)
        {
            _root.PreviewKeyDown -= OnPreviewKeyDown;
            _root.PreviewKeyUp -= OnPreviewKeyUp;
            _root.RemoveHandler(UIElement.PointerWheelChangedEvent, _wheelHandler);
        }

        _root = root;
        root.PreviewKeyDown += OnPreviewKeyDown;
        root.PreviewKeyUp += OnPreviewKeyUp;

        // Колесо над таблицей уже обработано ею (чтобы ScrollViewer не прокручивал) — нужны и обработанные события.
        root.AddHandler(UIElement.PointerWheelChangedEvent, _wheelHandler, handledEventsToo: true);
    }

    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!_isCapturing || IsDown(VirtualKey.Menu) || Translate(e.Key) is not { } key)
        {
            return;
        }

        var modifiers = KeyModifiers.None;
        if (IsDown(VirtualKey.Control))
        {
            modifiers |= KeyModifiers.Ctrl;
        }

        if (IsDown(VirtualKey.Shift))
        {
            modifiers |= KeyModifiers.Shift;
        }

        e.Handled = true;
        KeyPressed?.Invoke(this, new KeyInput(key, modifiers));
    }

    // Пара к KeyDown: кнопка в фокусе нажимается отпусканием Пробела — его тоже нужно забрать.
    private void OnPreviewKeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (_isCapturing && !IsDown(VirtualKey.Menu) && Translate(e.Key) is not null)
        {
            e.Handled = true;
        }
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (!_isCapturing || !e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
        {
            return;
        }

        var properties = e.GetCurrentPoint((UIElement)sender).Properties;
        if (properties.IsHorizontalMouseWheel || properties.MouseWheelDelta == 0)
        {
            return;
        }

        e.Handled = true;
        ZoomWheel?.Invoke(this, properties.MouseWheelDelta);
    }

    private static bool IsDown(VirtualKey key) =>
        InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
}
