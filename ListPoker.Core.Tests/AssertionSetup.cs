using System.Runtime.CompilerServices;

namespace ListPoker.Core.Tests;

internal static class AssertionSetup
{
    // FluentAssertions сравнивает записи через их Equals, а у ImmutableArray равенство ссылочное.
    // По полям BeEquivalentTo сравнивает массивы поэлементно — например, у сводок игры.
#pragma warning disable CA2255 // инициализатор модуля в тестовой сборке — так рекомендует FluentAssertions
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize() =>
        AssertionOptions.AssertEquivalencyUsing(options => options.ComparingRecordsByMembers());
}
