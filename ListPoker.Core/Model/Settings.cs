namespace ListPoker.Core.Model;

/// <summary>Настройки приложения.</summary>
/// <param name="TableScale">Масштаб таблицы в процентах, <see cref="MinScale"/>–<see cref="MaxScale"/> с шагом <see cref="ScaleStep"/>.</param>
/// <param name="StatsPlayerCount">Разбивка статистики: 3, 4, 5 или 6 игроков; <c>null</c> — все игры.</param>
/// <param name="Window">Размер и положение окна; <c>null</c> до первого запуска.</param>
public sealed record Settings(int TableScale, int? StatsPlayerCount, WindowBounds? Window)
{
    public const int MinScale = 75;
    public const int MaxScale = 200;
    public const int ScaleStep = 25;
    public const int DefaultScale = 100;

    public static Settings Default { get; } = new(DefaultScale, null, null);
}
