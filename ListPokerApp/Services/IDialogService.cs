namespace ListPokerApp.Services;

/// <summary>Простые диалоги поверх текущего экрана.</summary>
public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
    Task AlertAsync(string title, string message, string close = "Закрыть");
}
