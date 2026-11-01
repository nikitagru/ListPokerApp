using System.Collections.Immutable;

namespace ListPoker.Core.Rules;

/// <summary>Места по счёту.</summary>
public static class Placement
{
    /// <summary>
    /// Место каждого игрока со «стандартными» ничьими: равные делят место, следующее пропускается (1, 1, 3).
    /// Место — 1 + число игроков со счётом строго больше.
    /// </summary>
    public static ImmutableArray<int> Places(IReadOnlyList<int> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);
        return [.. scores.Select(score => 1 + scores.Count(other => other > score))];
    }
}
