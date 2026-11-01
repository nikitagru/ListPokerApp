using ListPoker.Core.Rules;

namespace ListPoker.Core.Tests.Rules;

public class PlacementTests
{
    [Theory]
    [InlineData(new[] { 10, 30, 20 }, new[] { 3, 1, 2 })] // без ничьих
    [InlineData(new[] { 30, 30, 10 }, new[] { 1, 1, 3 })] // ничья на первом месте
    [InlineData(new[] { 30, 20, 20, 10 }, new[] { 1, 2, 2, 4 })] // ничья в середине
    [InlineData(new[] { 5, 5, 5 }, new[] { 1, 1, 1 })] // все равны
    [InlineData(new[] { -10, 0, -30 }, new[] { 2, 1, 3 })] // отрицательные счета
    [InlineData(new[] { -5, -5, -40, 12, 12, 0 }, new[] { 4, 4, 6, 1, 1, 3 })] // шесть игроков, две ничьи
    public void Equal_scores_share_place_and_next_place_is_skipped(int[] scores, int[] expected)
    {
        Placement.Places(scores).Should().Equal(expected);
    }
}
