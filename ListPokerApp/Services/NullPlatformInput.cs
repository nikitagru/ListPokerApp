using ListPoker.Core.Session;

namespace ListPokerApp.Services;

/// <summary>Платформы без клавиатуры (телефон): событий нет, всё делается указателем.</summary>
public sealed class NullPlatformInput : IPlatformInput
{
    public event EventHandler<KeyInput>? KeyPressed
    {
        add { }
        remove { }
    }

    public event EventHandler<int>? ZoomWheel
    {
        add { }
        remove { }
    }

    public bool IsCapturing { get; set; }
}
