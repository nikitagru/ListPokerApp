namespace ListPokerApp.Controls.ScoreTable;

// Windows-часть таблицы: колесо мыши (Shift + колесо, колесо над закреплёнными частями).
public sealed partial class ScoreTableView
{
    private TableWheelScroll? _wheel;

    partial void AttachPlatformInput() =>
        _wheel ??= TableWheelScroll.TryAttach(_scroll, _body, _parts.Where(part => part != _body));

    partial void DetachPlatformInput()
    {
        _wheel?.Detach();
        _wheel = null;
    }
}
