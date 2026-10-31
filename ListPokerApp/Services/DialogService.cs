namespace ListPokerApp.Services;

public sealed class DialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        CurrentPage.DisplayAlertAsync(title, message, accept, cancel);

    public Task AlertAsync(string title, string message, string close = "Закрыть") =>
        CurrentPage.DisplayAlertAsync(title, message, close);

    private static Page CurrentPage =>
        Shell.Current?.CurrentPage
        ?? throw new InvalidOperationException("Нет активной страницы для показа диалога.");
}
