namespace ListPoker.Core.Model;

/// <summary>Размер и положение окна, в пикселях экрана.</summary>
/// <param name="Width">Ширина, не меньше <see cref="MinWidth"/>.</param>
/// <param name="Height">Высота, не меньше <see cref="MinHeight"/>.</param>
/// <param name="Maximized">Окно развёрнуто на весь экран.</param>
public sealed record WindowBounds(int X, int Y, int Width, int Height, bool Maximized)
{
    public const int MinWidth = 1024;
    public const int MinHeight = 640;
}
