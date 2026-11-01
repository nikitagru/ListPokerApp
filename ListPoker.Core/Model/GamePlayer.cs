namespace ListPoker.Core.Model;

/// <summary>Игрок в игре.</summary>
/// <param name="Name">Имя на момент создания игры (копия), 1–<see cref="Player.MaxNameLength"/> символов.</param>
/// <param name="PlayerId">Ссылка на постоянного игрока; может указывать на удалённого.</param>
public sealed record GamePlayer(string Name, Guid PlayerId);
