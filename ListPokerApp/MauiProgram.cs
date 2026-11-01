using CommunityToolkit.Maui;
using ListPokerApp.Pages;
using ListPokerApp.Services;
using ListPokerApp.ViewModels;
using Microsoft.Extensions.Logging;

namespace ListPokerApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        RegisterServices(builder.Services);
        RegisterPages(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    // Сервисы живут всё время работы приложения
    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<INavigationService, ShellNavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
    }

    // Страницы и ViewModel создаются заново при каждом переходе
    private static void RegisterPages(IServiceCollection services)
    {
        services.AddTransient<StartPage>().AddTransient<StartViewModel>();
        services.AddTransient<NewGamePage>().AddTransient<NewGameViewModel>();
        services.AddTransient<GamePage>().AddTransient<GameViewModel>();
        services.AddTransient<PlayersPage>().AddTransient<PlayersViewModel>();
        services.AddTransient<PlayerGamesPage>().AddTransient<PlayerGamesViewModel>();
#if DEBUG
        services.AddTransient<Prototype.PrototypePage>();
#endif
    }
}
