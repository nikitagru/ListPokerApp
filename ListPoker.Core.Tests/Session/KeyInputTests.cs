using ListPoker.Core.Session;

namespace ListPoker.Core.Tests.Session;

public class KeyInputTests
{
    [Theory]
    [InlineData(InputKey.Digit0, 0)]
    [InlineData(InputKey.Digit7, 7)]
    [InlineData(InputKey.Digit9, 9)]
    public void Digit_keys_give_their_digit(InputKey key, int expected)
    {
        new KeyInput(key).Digit.Should().Be(expected);
    }

    [Theory]
    [InlineData(InputKey.Enter)]
    [InlineData(InputKey.Plus)]
    [InlineData(InputKey.Z)]
    public void Other_keys_have_no_digit(InputKey key)
    {
        new KeyInput(key).Digit.Should().BeNull();
    }

    [Fact]
    public void Modifiers_are_reported_separately()
    {
        var input = new KeyInput(InputKey.Tab, KeyModifiers.Ctrl | KeyModifiers.Shift);

        input.Ctrl.Should().BeTrue();
        input.Shift.Should().BeTrue();
        new KeyInput(InputKey.Tab).Ctrl.Should().BeFalse();
    }

    [Theory]
    [InlineData(InputKey.Tab, KeyModifiers.Shift, "Shift+Tab")]
    [InlineData(InputKey.Digit0, KeyModifiers.Ctrl, "Ctrl+0")]
    [InlineData(InputKey.Z, KeyModifiers.Ctrl | KeyModifiers.Shift, "Ctrl+Shift+Z")]
    [InlineData(InputKey.F2, KeyModifiers.None, "F2")]
    public void Text_lists_modifiers_then_key(InputKey key, KeyModifiers modifiers, string expected)
    {
        new KeyInput(key, modifiers).ToString().Should().Be(expected);
    }
}
