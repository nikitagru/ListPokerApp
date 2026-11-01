using ListPokerApp.ViewModels;

namespace ListPokerApp.Pages;

public partial class StartPage : ContentPage
{
    public StartPage(StartViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
#if DEBUG
        // Кнопка прототипа (секция 1) — только в DEBUG, поэтому не в XAML.
        Root.Add(new Button
        {
            Text = "Прототип таблицы",
            AutomationId = "OpenPrototype",
            HorizontalOptions = LayoutOptions.Start,
            Command = viewModel.OpenPrototypeCommand,
        });
#endif
    }
}
