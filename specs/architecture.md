# Архитектура

Как устроено приложение: проекты, слои, ключевые классы и их ответственность, работа с платформой, план реализации. Что приложение делает — в остальных документах; здесь — как.

## 1. Принципы

1. **Вся логика — в чистой библиотеке без MAUI.** Правила, подсчёт, проверки ввода, порядок фокуса, отмена/повтор, статистика, хранение в JSON и CSV-экспорт живут в `ListPoker.Core` (`net10.0`). Ссылки на MAUI там нет, поэтому зависимость логики от интерфейса невозможна — это проверяет компилятор.
2. **Данные игры неизменяемы.** `Game` и вложенные типы — `record` с неизменяемыми коллекциями. Любое изменение создаёт новый `Game`. Отсюда бесплатно получаются отмена/повтор (стек снимков), безопасное сохранение в фоне и простое сравнение «до/после».
3. **Хранится только ввод, всё остальное вычисляется.** Очки, счёт, места, активная раздача, порядок заказов, запрещённый заказ, ошибки строк, статистика — функции от сохранённых данных. Игра маленькая (≤ 28 раздач × 6 игроков), пересчёт целиком после каждого изменения стоит микросекунды.
4. **Интерфейс тонкий.** Страница получает готовую «модель отображения» и рисует её; решения «можно ли», «что дальше», «какой текст подсказки» принимает Core.
5. **Платформа — за интерфейсами.** Клавиатура, колесо мыши, окно, диалог сохранения файла, папка данных — через сервисы с реализацией в `Platforms/Windows`.

## 2. Решение и проекты

```
ListPoker/
├── ListPoker.slnx
├── specs/                         документация
├── ListPoker.Core/                net10.0, без MAUI
├── ListPoker.Core.Tests/          xUnit, net10.0
└── ListPokerApp/                  .NET MAUI, сейчас собирается только под Windows
```

Корень git-репозитория — папка `ListPoker/`. Общие настройки сборки всех проектов (`Nullable`, `ImplicitUsings`, `LangVersion`, `TreatWarningsAsErrors`) заданы в `ListPoker/Directory.Build.props`.

| Проект | Ссылается на | Содержимое |
|--------|--------------|------------|
| `ListPoker.Core` | — | Модель, правила, вычисления, сессия ввода, модель таблицы, статистика, хранение, CSV |
| `ListPoker.Core.Tests` | Core | Модульные тесты логики и хранения |
| `ListPokerApp` | Core | Страницы, ViewModel, отрисовка таблицы, PNG, платформенные сервисы, DI |

В `ListPokerApp.csproj` целевые платформы остаются в файле, но по умолчанию собирается только `net10.0-windows10.0.19041.0` — остальные включаются при переносе на телефон.

Внешние пакеты:

| Пакет | Где | Зачем |
|-------|-----|-------|
| `CommunityToolkit.Mvvm` | App | `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` |
| `CommunityToolkit.Maui` | App | Всплывающие окна (итоги, экспорт), `FileSaver` — кроссплатформенный «Сохранить как» |
| `Microsoft.Maui.Graphics.Skia` | App | Отрисовка таблицы в PNG вне экрана |
| `xunit`, `FluentAssertions` | Tests | Тесты |

## 3. Слои

```
┌──────────────────────────────────────────────────────────────────┐
│ ListPokerApp                                                     │
│  Pages (XAML) ──▶ ViewModels ──▶ Controls/ScoreTable (рендер)    │
│        │              │                    ▲                     │
│        │              ▼                    │ TableState          │
│  Platforms/Windows ◀─ IPlatformInput, IWindowService, ...        │
├──────────────────────────────┬───────────────────────────────────┤
│ ListPoker.Core               ▼                                   │
│  Services:  GameLibrary · PlayerDirectory · SettingsService      │
│  Session:   GameSession (ввод, правка, undo) · InputRoute        │
│  Table:     TableStateBuilder · HintBuilder                      │
│  Calculation: GameCalculator → GameSummary                       │
│  Statistics · Export(CSV)                                        │
│  Rules:     RoundPlanner · Scoring · BidRules · Placement        │
│  Model:     Game · Round · PlayerRound · Player · Settings       │
│  Storage:   JSON-файлы, атомарная запись                         │
└──────────────────────────────────────────────────────────────────┘
```

Зависимости направлены только вниз: `Rules` знает только `Model`; `Calculation` — `Rules`; `Session` — `Calculation`; и так далее.

## 4. ListPoker.Core

### 4.1. Model

Типы из [data-storage.md](data-storage.md) — один к одному, как `sealed record`:

```csharp
public sealed record Game(
    Guid Id, DateTimeOffset CreatedAt, DateTimeOffset? FinishedAt, bool FinishedEarly,
    ImmutableArray<GamePlayer> Players, int FirstDealerIndex, ImmutableArray<Round> Rounds);

public sealed record Round(
    int Number, int Cards, RoundStage Stage, int DealerIndex,
    ImmutableArray<PlayerRound> Entries, bool Closed);

public sealed record PlayerRound(int? Bid, int? Tricks, bool Dark);
```

Изменения — через вспомогательные методы, возвращающие новый объект: `game.WithEntry(round, player, e => e with { Bid = 3 })`, `game.WithRound(...)`. Методы модели ничего не проверяют — проверки в `Rules` и `Session`.

`Closed` хранится, как требует модель данных, но выставляет его только `GameSession`, и только при выполнении условий закрытия. Поэтому оно всегда совпадает с «все взятки введены, `Σ заказов ≠ K`, `Σ взяток = K`».

### 4.2. Rules — правила игры

Статические чистые функции, по одной теме на класс. Это прямое отражение [gamerules.md](gamerules.md); здесь же сосредоточены тесты крайних случаев.

| Класс | Функции |
|-------|---------|
| `RoundPlanner` | `MaxCards(n)`, `RoundCount(n)`, `BuildRounds(n, firstDealer)` — список раздач с K, этапом, раздающим, `dark` в финале |
| `Scoring` | `Score(bid, tricks, dark)`, `Outcome(bid, tricks)` → `Exact` / `Over` / `Under`, `Explain(...)` для всплывающей расшифровки |
| `BidRules` | `BidOrder(round)` — порядок заказов с учётом «тёмных»; `TrickOrder(round)`; `ForbiddenDealerBid(round)`; `CanToggleDark(round, player)` |
| `Placement` | `Places(scores)` — места с ничьими (1, 1, 3) |

### 4.3. Calculation — вычисляемое состояние игры

`GameCalculator.Calculate(Game) → GameSummary` — один проход по раздачам:

```csharp
public sealed record GameSummary(
    GameStatus Status,               // InProgress / Finished / FinishedEarly
    int? ActiveRoundIndex,           // первая незакрытая; null в завершённой игре
    int ClosedRoundCount,
    ImmutableArray<RoundSummary> Rounds,
    ImmutableArray<int> TotalScores, // по закрытым раздачам
    ImmutableArray<int>? Places);    // null до закрытия первой раздачи

public sealed record RoundSummary(
    ImmutableArray<int?> Scores,     // очки за раздачу, null у незакрытой
    ImmutableArray<int?> Cumulative, // накопленный счёт
    ImmutableArray<RowError> Errors);// «Сумма заказов равна K», «Сумма взяток 4, должно быть 3»
```

`GameSummary` используют и таблица, и список игр (состояние, победитель), и статистика — одна формула на всё приложение.

### 4.4. Session — ввод в таблицу

`GameSession` — центральный класс экрана таблицы. Он создаётся при открытии игры и живёт, пока открыт экран. Держит текущий `Game`, его `GameSummary`, строку на правке и историю отмены.

```csharp
public sealed class GameSession
{
    public Game Game { get; }
    public GameSummary Summary { get; }
    public RowEdit? Edit { get; }            // строка на правке и её черновик
    public bool CanUndo { get; }
    public bool CanRedo { get; }

    public event EventHandler<GameChangedEventArgs>? Changed;

    public InputResult SetBid(int round, int player, int? value);    // null — очистить
    public InputResult SetTricks(int round, int player, int? value);
    public InputResult ToggleDark(int round, int player);

    public InputResult BeginEdit(int round);
    public InputResult ApplyEdit();
    public void CancelEdit();

    public InputResult FinishEarly();
    public InputResult Resume();

    public void Undo();
    public void Redo();
}
```

**Каждый метод — одно действие пользователя.** Метод либо отклоняет его (`InputResult.Rejected` с текстом для строки подсказки и, при необходимости, ячейками-виновниками), либо принимает: строит новый `Game`, кладёт прежний в историю отмены, пересчитывает `Summary`, выпускает `Changed`.

Что проверяется при вводе (таблица из [score-table.md §6](score-table.md#6-проверки)):

- диапазон `0…K`;
- заказ раздающего — только после всех остальных и не равен запрещённому;
- взятки — только когда введены все заказы и `Σ заказов ≠ K`;
- «в тёмную» — не у раздающего вне финала, не в финале, не после ввода взяток;
- ввод — только в активную раздачу или в строку на правке; пока строка на правке, активная раздача недоступна.

**Закрытие раздачи.** Если после `SetTricks` все взятки введены и проверки пройдены, тот же шаг ставит `Closed = true`. Ввод последней взятки и закрытие — одно действие, поэтому его отмена заново открывает раздачу, как требует [§8](score-table.md#8-отмена-и-повтор).

**Правка.** `BeginEdit` копирует раздачу в черновик `RowEdit`. `SetBid`/`SetTricks`/`ToggleDark` для этой строки меняют черновик и не трогают `Game` и историю. `ApplyEdit` проверяет черновик целиком (`Σ заказов ≠ K`, `Σ взяток = K`) и коммитит его одним действием. Пока строка на правке, `Undo`/`Redo` и досрочное завершение недоступны (см. [open-questions](open-questions.md#согласованные-правила-поведения), п. 19).

**Отмена и повтор.** `UndoHistory` — два стека снимков `Game`. Новое действие очищает стек повтора. Снимки неизменяемы, поэтому копирование не нужно. Глубина ограничена (500 шагов) — с запасом на целую партию.

**`GameChangedEventArgs`** сообщает: прежний и новый `Game`/`Summary`, `IsCommitted` (сохранять или это изменение черновика), `StatusChanged` (игра только что завершилась — показать итоги). По разнице `Summary` экран подсвечивает изменившиеся ячейки «Счёт».

### 4.5. Session — маршрут фокуса и ввод числа

Эти две вещи — тоже логика, а не интерфейс, и на телефоне понадобятся без изменений.

**`InputRoute`** — куда переходит фокус. По `Game` и текущей ячейке (`CellRef(Round, Player, CellKind.Bid | CellKind.Tricks)`) возвращает следующую и предыдущую ячейку для `Enter`/`Tab`/`Shift+Tab`: незаполненные заказы в порядке `BidRules.BidOrder`, затем взятки в порядке `TrickOrder`. Там же — номер в очереди для подсказки в пустых ячейках «З» и **предлагаемое значение** для последней незаполненной ячейки «В» (остаток до K). Предложенное значение не попадает в `Game`, пока его не подтвердят.

**`NumberEntry`** — буфер набора числа в ячейке. Принимает цифру и K, отвечает: `Commit(value)` — значение однозначно, принять сразу; `Wait` — ждать вторую цифру или `Enter` (цифра `1` при K ≥ 10); `Reject` — вне диапазона. Буфер не сохраняется и сбрасывается по `Esc`.

### 4.6. Table — модель отображения таблицы

`TableStateBuilder.Build(session, focus, entryBuffer) → TableState` — плоское описание того, что нарисовать:

- для каждой ячейки: текст, вид (`Future` / `Active` / `Closed` / `Editing`), исход (`Exact` / `Over` / `Under`) для цвета «Оч», отметка ●, запрет `✕n`, номер очереди, предложенное значение, признак ошибки, признак фокуса;
- для строк: этап и граница этапа, маркер ▶, значок правки, линия «Игра завершена досрочно»;
- шапка: имена, пометка «сдаёт», выделение лидеров;
- итоговые строки: счёт и места.

`HintBuilder` строит текст строки подсказки: «Заказывает Вика (раздающий). Нельзя заказать 1.», «Осталось распределить взяток: 2», текст ошибки.

Цвета в `TableState` не задаются — только смысловые признаки. Палитра — в интерфейсе. Так `TableState` полностью покрывается тестами без MAUI, а рендер остаётся «глупым».

### 4.7. Statistics

`StatisticsCalculator.Calculate(games, players, playerCount?) → IReadOnlyList<PlayerStats>` — показатели из [screens.md §4](screens.md#статистика), по завершённым играм, у досрочно завершённых — только закрытые раздачи. `PlayerGames(...)` — список игр одного игрока для перехода из строки статистики. Ничего не кэширует и не хранит.

### 4.8. Storage

```csharp
public interface IGameStore
{
    Task<GameLoadResult> LoadAllAsync(CancellationToken ct);  // игры + список нечитаемых файлов
    Task SaveAsync(Game game, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
public interface IPlayerStore   { Task<ImmutableArray<Player>> LoadAsync(...); Task SaveAsync(...); }
public interface ISettingsStore { Task<Settings> LoadAsync(...);               Task SaveAsync(...); }
```

Реализация — `JsonFileStore` поверх `System.IO`; он работает на всех платформах, платформенный только корневой путь. Его передаёт приложение (`FileSystem.AppDataDirectory`), Core путь не вычисляет.

```
<AppData>/
├── games/<id>.json
├── players.json
└── settings.json
```

- `System.Text.Json` с source generator (`ListPokerJsonContext`), имена полей в camelCase, как в модели данных.
- В корне каждого файла — `formatVersion: 1`, чтобы будущие изменения формата можно было мигрировать.
- **Атомарная запись** `AtomicFile.WriteAsync`: запись в `<name>.tmp`, `Flush(true)`, затем `File.Move(tmp, target, overwrite: true)`. При сбое на любом шаге прежний файл цел.
- Нечитаемый файл игры не удаляется и не перезаписывается — пропускается при загрузке, приложение сообщает «Не удалось прочитать N игр».

### 4.9. Services — состояние приложения

Синглтоны, живут всё время работы приложения.

| Сервис | Ответственность |
|--------|-----------------|
| `GameLibrary` | Все игры в памяти (загружаются один раз при запуске) и их `GameSummary`. `Create`, `Update`, `Delete`. Событие `GamesChanged`. Источник для списка игр, «Продолжить» и статистики |
| `GameSaver` | Очередь записи: последний снимок каждой игры пишется в фоне, ровно один писатель на файл, промежуточные снимки отбрасываются. Свойство `LastError` — для предупреждения «Не удалось сохранить» и повтор при следующем изменении |
| `PlayerDirectory` | Постоянные игроки: добавить, переименовать, удалить, уникальность имени без учёта регистра, автодобавление новых имён из формы новой игры |
| `SettingsService` | Масштаб, выбранная разбивка статистики, окно. Сохраняет при изменении |

Поток при вводе значения:

```
клавиша ─▶ GameViewModel ─▶ GameSession.SetBid ─▶ Changed(IsCommitted)
                                                      │
                     TableState пересобран ◀──────────┤
                                                      ▼
                                   GameLibrary.Update(game) ─▶ GameSaver (фон)
```

Загрузка всех игр в память — сознательное упрощение: файл игры — единицы килобайт, даже тысяча партий читается за доли секунды, а статистика всё равно нужна по всем играм.

### 4.10. Export — CSV

`CsvExporter.Write(game, summary, Stream)` — формат из [score-table.md §11](score-table.md#11-экспорт): разделитель `;`, UTF-8 с BOM. `ExportFileName.For(game)` — имя по умолчанию `Покер 2026-10-12 Аня-Боря-Вика` с заменой недопустимых в имени файла символов.

## 5. ListPokerApp — интерфейс

### 5.1. MVVM и навигация

- Страницы — XAML + ViewModel на `CommunityToolkit.Mvvm`. Код в `*.xaml.cs` — только то, что невозможно в привязках (подписка на платформенный ввод, прокрутка к активной строке).
- Навигация — `Shell` с маршрутами, обёрнутая в `INavigationService` (`GoToStart`, `GoToNewGame`, `GoToGame(id)`, `GoToPlayers`, `GoToPlayerGames(id)`), чтобы ViewModel не зависели от `Shell`.
- Подтверждения — `IDialogService` поверх `DisplayAlert`; итоги игры и выбор формата экспорта — всплывающие окна `CommunityToolkit.Maui`.
- DI в `MauiProgram`: сервисы Core и платформенные сервисы — синглтоны, страницы и ViewModel — transient. `GameSession` создаётся `GameViewModel` при открытии игры, а не через контейнер.

| Страница | ViewModel | Что использует |
|----------|-----------|----------------|
| `StartPage` | `StartViewModel` | `GameLibrary` — список игр, «Продолжить», удаление |
| `NewGamePage` | `NewGameViewModel` | `PlayerDirectory`, `RoundPlanner` (сводка), `GameLibrary.Create` |
| `GamePage` | `GameViewModel` | `GameSession`, `InputRoute`, `NumberEntry`, `TableStateBuilder`, `SettingsService` |
| `PlayersPage` | `PlayersViewModel` | `PlayerDirectory`, `StatisticsCalculator`, `SettingsService` |
| `PlayerGamesPage` | `PlayerGamesViewModel` | `StatisticsCalculator.PlayerGames` |

### 5.2. GameViewModel

Связывает ввод пользователя с `GameSession` и хранит то, что относится к экрану, а не к игре:

- `Focus` — текущая ячейка; `NumberEntry` — набираемое число;
- `TableState` — пересобирается после каждого изменения сессии, фокуса или буфера;
- `HintText`, `IsSaveFailed`, `CanUndo`, `CanRedo`, `Scale`, текст верхней панели;
- команды для кнопок панели и для всех клавиш — клавиши лишь вызывают те же команды, что и указатель.

Обработка клавиши: `KeyInput` → команда → вызов `GameSession` → при `Rejected` текст ошибки в подсказку, ячейка остаётся в режиме ввода; при успехе фокус переходит по `InputRoute.Next`.

### 5.3. Таблица — свой рендер

Таблица рисуется вручную через `Microsoft.Maui.Graphics`: штатные списки и сетки MAUI плохо справляются с закреплёнными областями, масштабом всей таблицы и снимком таблицы целиком в PNG.

**Части.** Контрол `ScoreTableView` собран из пяти `GraphicsView` и одного `ScrollView`:

```
┌──────────┬───────────────────────────────┐
│ Угол     │ Шапка          — сдвиг по X   │
├──────────┼───────────────────────────────┤
│ Служеб-  │ Тело в ScrollView             │
│ ные      │ (прокрутка X и Y — штатная)   │
│ столбцы  │                               │
│ сдвиг Y  │                               │
├──────────┼───────────────────────────────┤
│ Угол     │ Итоги          — сдвиг по X   │
└──────────┴───────────────────────────────┘
```

Тело лежит в `ScrollView` и имеет полный размер таблицы. Остальные части обрезаются по своей области и сдвигаются (`TranslationX/Y`) по событию `ScrollView.Scrolled`. Так прокрутка, полосы прокрутки, колесо мыши, а позже и касание на телефоне — штатные. Самим реализуется только синхронизация смещений.

**Классы.**

| Класс | Ответственность |
|-------|-----------------|
| `TableLayout` | Геометрия: ширины столбцов и высоты строк из базовых размеров × масштаб; прямоугольник любой ячейки; `HitTest(point) → CellRef` с зоной значка ●; прямоугольник для прокрутки к активной строке |
| `TableRenderer` | Рисует `TableState` по `TableLayout` в `ICanvas` заданной области. Не знает ни о сессии, ни об экране |
| `TableTheme` | Палитра и шрифты: фон этапов, подсветка активной строки и правки, зелёный/красный, выделение раздающего и лидера |
| `TableDrawable` | `IDrawable` для одной части: вызывает `TableRenderer` для своей области |

Ширины столбцов задаются в теме и не зависят от длины текста: имена в шапке обрезаются многоточием. Измерять текст не нужно, и экран совпадает с PNG.

**Указатель.** `TapGestureRecognizer` (один и два щелчка) и `PointerGestureRecognizer` на теле → `TableLayout.HitTest` → команда ViewModel. Всплывающая расшифровка «Оч» — элемент-оверлей поверх таблицы, позиционируется по прямоугольнику ячейки.

**Масштаб.** Меняет только коэффициент в `TableLayout`; верхняя панель и подсказка — обычные контролы и не масштабируются. После смены масштаба активная строка возвращается в видимую область.

**PNG.** `PngExporter` создаёт `SkiaBitmapExportContext` размером со всю таблицу при масштабе 100% и вызывает тот же `TableRenderer` для всех частей без сдвигов. Экран и файл рисует один код.

### 5.4. Платформенные сервисы

| Интерфейс | Назначение | Windows |
|-----------|------------|---------|
| `IPlatformInput` | События `KeyPressed(KeyInput)` и `ZoomWheel(delta)`; `KeyInput` — платформенно-нейтральные клавиша и модификаторы | Подписка на `PreviewKeyDown` и `PointerWheelChanged` корневого элемента окна WinUI. Пока активна страница таблицы, обработанные клавиши (`Tab`, стрелки, `Пробел`) помечаются `Handled`, чтобы WinUI не уводил фокус |
| `IWindowService` | Минимальный размер, восстановление и запоминание положения, размера и развёрнутости окна | `Window.MinimumWidth/Height`, `X/Y/Width/Height`; развёрнутость — через `OverlappedPresenter` |
| `IFileSaver` | Диалог «Сохранить как» | `CommunityToolkit.Maui.Storage.FileSaver` (уже кроссплатформенный) |
| `IAppDataLocation` | Корневая папка данных | `FileSystem.AppDataDirectory` (кроссплатформенный) |

На телефоне `IPlatformInput` просто не выдаёт событий — всё доступно указателем, как требует [app-overview.md §4](app-overview.md#технология).

## 6. Потоки и ошибки

- Вся логика и все изменения — в UI-потоке. В фоне — только чтение файлов при запуске и запись в `GameSaver`. Снимки `Game` неизменяемы, поэтому передаются в фон без копирования и блокировок.
- Ошибка записи не прерывает работу: `GameSaver.LastError` → заметная плашка на экране таблицы; следующее изменение повторяет запись.
- Отклонённый ввод — не исключение, а `InputResult.Rejected`. Исключения — только для ошибок программы.
- Необработанные исключения пишутся в лог (`Microsoft.Extensions.Logging`, файл в папке данных) и показываются диалогом.

## 7. Тестирование

| Что | Как |
|-----|-----|
| `Rules`, `Calculation` | Таблицы примеров из [gamerules.md](gamerules.md) и все «тесты крайних случаев» оттуда; число раздач для N = 3…6 |
| `GameSession` | Сценарные тесты: полная партия на 3 и 6 игроков, запрещённый заказ и запрет паса, вне очереди, «в тёмную», ошибка `Σ заказов = K` после правки заказа, закрытие и его отмена, правка с применением и отменой, досрочное завершение и возобновление, undo/redo с обрывом цепочки |
| `InputRoute`, `NumberEntry` | Порядок фокуса с «тёмными», автоподтверждение при K ≤ 9 и K ≥ 10, подстановка остатка |
| `TableStateBuilder`, `HintBuilder` | Состояния строк и тексты подсказок в ключевых точках партии |
| `Statistics` | Ничьи, досрочные игры, разбивка по числу игроков, удалённый игрок |
| `Storage` | Круговое сохранение/загрузка, нечитаемый файл, атомарная запись (временный файл не портит основной) |
| `CsvExporter` | Сравнение с эталонным файлом |
| Интерфейс | Ручной контрольный список по [score-table.md](score-table.md) |

## 8. Соответствие требованиям

| Требование | Где реализовано |
|------------|-----------------|
| Список раздач, раздающий, этапы | `RoundPlanner` |
| Очки, счёт, места | `Scoring`, `Placement`, `GameCalculator` |
| Порядок заказов, запрещённый заказ | `BidRules`, `InputRoute` |
| Проверки ввода, закрытие раздачи | `GameSession` |
| Правка закрытой раздачи | `GameSession` (`RowEdit`) |
| Отмена и повтор | `GameSession` (`UndoHistory`) |
| Досрочное завершение и возобновление | `GameSession` |
| Автосохранение без потери данных | `GameLibrary`, `GameSaver`, `AtomicFile` |
| Статистика | `StatisticsCalculator` |
| Закреплённые области, масштаб | `ScoreTableView`, `TableLayout` |
| Ввод с клавиатуры | `IPlatformInput` → `GameViewModel` → `NumberEntry` |
| Экспорт CSV / PNG | `CsvExporter` / `PngExporter` + `IFileSaver` |
| Размер и положение окна | `IWindowService`, `SettingsService` |

## 9. Риски

| Риск | Что делаем |
|------|-----------|
| WinUI перехватывает `Tab` и стрелки для своей навигации фокусом | Прототип перехвата в `PreviewKeyDown` — первым делом, до основной работы |
| Синхронная прокрутка пяти частей дёргается | Прототип на 6 игроках × 28 строк при 200%; запасной вариант — одна `GraphicsView` на весь видимый прямоугольник со своей прокруткой |
| Шрифты Win2D (экран) и Skia (PNG) различаются в мелочах | Допустимо: столбцы фиксированной ширины, текст не измеряется |
| Skia на Windows не заработает | Запасной вариант — `PngExporter` через Win2D за тем же интерфейсом |

## 10. План реализации

Каждый этап заканчивается работающей сборкой и зелёными тестами.

| # | Этап | Результат |
|:-:|------|-----------|
| 0 | Каркас | Git, решение на уровне `ListPoker/`, проекты Core и Tests, сборка только под Windows, пакеты, пустой `Shell` с маршрутами |
| 1 | Прототип рисков | `ScoreTableView` на фиктивных данных: пять частей, прокрутка, масштаб, щелчки, перехват клавиш, PNG через Skia. Решение по запасным вариантам из [§9](#9-риски) |
| 2 | Правила и вычисления | `Model`, `Rules`, `GameCalculator` с тестами |
| 3 | Сессия ввода | `GameSession`, `UndoHistory`, `InputRoute`, `NumberEntry` с тестами |
| 4 | Хранение | `JsonFileStore`, `AtomicFile`, `GameLibrary`, `GameSaver`, `PlayerDirectory`, `SettingsService` с тестами |
| 5 | Стартовый экран и новая игра | Создание, список, «Продолжить», удаление |
| 6 | Таблица игры | `TableStateBuilder`, `HintBuilder`, `GameViewModel`, рендер на реальных данных, весь ввод, правка, отмена, итоги, досрочное завершение |
| 7 | Игроки и статистика | `PlayersPage`, `PlayerGamesPage`, разбивка по числу игроков |
| 8 | Экспорт | CSV и PNG, диалог экспорта, `Ctrl+E` |
| 9 | Доводка | Окно, всплывающая расшифровка очков, подсветка изменившегося счёта, проход по контрольному списку |
