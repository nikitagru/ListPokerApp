namespace ListPokerApp.Controls.ScoreTable;

/// <summary>Часть таблицы. Каждая рисуется своей <see cref="GraphicsView"/> в собственных координатах.</summary>
public enum TablePart
{
    /// <summary>Угол сверху: заголовки служебных столбцов. Не двигается.</summary>
    TopLeft,

    /// <summary>Шапка: имена игроков и подстолбцы. Сдвигается по X вместе с телом.</summary>
    Header,

    /// <summary>Служебные столбцы строк раздач. Сдвигаются по Y вместе с телом.</summary>
    Side,

    /// <summary>Тело: ячейки игроков в строках раздач. Прокручивается штатно.</summary>
    Body,

    /// <summary>Угол снизу: подписи итоговых строк. Не двигается.</summary>
    BottomLeft,

    /// <summary>Итоговые строки «Счёт» и «Место». Сдвигаются по X вместе с телом.</summary>
    Footer,
}

/// <summary>Служебный столбец слева.</summary>
public enum ServiceColumn
{
    /// <summary>№ раздачи.</summary>
    Number,

    /// <summary>K — карт у каждого игрока.</summary>
    Cards,

    /// <summary>Имя раздающего.</summary>
    Dealer,
}

/// <summary>Подстолбец блока игрока.</summary>
public enum PlayerColumn
{
    /// <summary>З — заказ.</summary>
    Bid,

    /// <summary>В — взятки.</summary>
    Tricks,

    /// <summary>Оч — очки за раздачу.</summary>
    Points,

    /// <summary>Счёт — накопленный счёт.</summary>
    Score,
}
