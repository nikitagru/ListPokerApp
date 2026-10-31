using CommunityToolkit.Mvvm.Input;
using ListPokerApp.Services;

namespace ListPokerApp.ViewModels;

public sealed partial class NewGameViewModel(INavigationService navigation) : ViewModelBase
{
    // Временно, до секции 5: «Начать» без формы.
    [RelayCommand]
    private Task Start() => navigation.GoToGame(Guid.Empty);

    [RelayCommand]
    private Task Cancel() => navigation.GoToStart();
}
