namespace ListPoker.Core.Model;

/// <summary>Запись игрока в раздаче.</summary>
/// <param name="Bid">Заказ, 0…K; <c>null</c>, пока не введён.</param>
/// <param name="Tricks">Взятки, 0…K; <c>null</c>, пока не введены.</param>
/// <param name="Dark">Играет в тёмную. В финальном плато всегда <c>true</c>.</param>
public sealed record PlayerRound(int? Bid, int? Tricks, bool Dark)
{
    /// <summary>Пустая запись: ничего не введено, не в тёмную.</summary>
    public static PlayerRound Empty { get; } = new(null, null, false);
}
