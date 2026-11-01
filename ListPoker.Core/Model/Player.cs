namespace ListPoker.Core.Model;

/// <summary>Постоянный игрок.</summary>
/// <param name="Id">Уникальный идентификатор. Статистика связана с игроком по нему, а не по имени.</param>
/// <param name="Name">Имя, 1–<see cref="MaxNameLength"/> символов, уникально без учёта регистра.</param>
public sealed record Player(Guid Id, string Name)
{
    public const int MaxNameLength = 20;
}
