using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Nova.Core;
using Nova.Formats;
using Nova.Formula;
using Nova.UI.Shared;
using Serilog;

namespace Nova.Grid;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static Window? MainWindow { get; private set; }
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeLogging();
        InitializeComponent();
        ConfigureServices();
    }

    private void InitializeLogging()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NovaOffice",
            "Logs",
            "Nova.Grid-.log");

        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .CreateLogger();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core services
        services.AddNovaCore("Nova.Grid");

        // Formula
        services.AddNovaFormula();

        // Formats
        services.AddNovaFormats();

        // UI Shared
        services.AddSingleton<ThemeService>();
        services.AddSingleton<ICommandPaletteService, CommandPaletteService>();
        services.AddSingleton<IRecentFilesService>(sp => sp.GetRequiredService<IRecentFilesService>());

        // Grid-specific services
        services.AddSingleton<IDocumentFactory, GridDocumentFactory>();
        services.AddSingleton<ISpreadsheetContext, SpreadsheetContext>(sp =>
            new SpreadsheetContext(sp.GetRequiredService<FormulaEngine>()));

        Services = services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow(args.Arguments);
        MainWindow.Activate();

        var themeService = Services.GetRequiredService<ThemeService>();
        themeService.InitializeWindow(MainWindow);
    }
}

/// <summary>
/// Main window for NOVA Grid.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ThemeService _themeService;
    private readonly ICommandPaletteService _commandPaletteService;
    private readonly CommandPalette _commandPalette;
    private readonly CommandPaletteShortcutHandler _shortcutHandler;
    private readonly string[] _args;

    public MainWindow(string[] args)
    {
        _args = args;
        _themeService = App.Services.GetRequiredService<ThemeService>();
        _commandPaletteService = App.Services.GetRequiredService<ICommandPaletteService>();

        InitializeComponent();
        InitializeTitleBar();
        InitializeCommandPalette();
        LoadContent();
    }

    private void InitializeComponent()
    {
        Title = "NOVA Grid";
        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
        AppWindow.Resize(new SizeInt32(1400, 900));
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }

    private void InitializeTitleBar()
    {
        var titleBar = new CustomTitleBar();
        titleBar.AppIcon = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/GridIcon.png"));
        titleBar.DocumentTitle = "Untitled";
        titleBar.SaveStatus = SaveStatus.Saved;
        titleBar.Initialize(this, _themeService);

        var grid = Content as Grid;
        if (grid != null)
        {
            grid.RowDefinitions.Insert(0, new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(titleBar, 0);
            grid.Children.Add(titleBar);
        }
    }

    private void InitializeCommandPalette()
    {
        _commandPalette = new CommandPalette(_commandPaletteService);
        _shortcutHandler = new CommandPaletteShortcutHandler(_commandPalette, this);
        RegisterGridCommands();
    }

    private void RegisterGridCommands()
    {
        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "grid.new",
            Title = "New Spreadsheet",
            Description = "Create a new blank spreadsheet",
            Glyph = "&#xE710;",
            Category = "File",
            Shortcut = "Ctrl+N",
            Command = new RelayCommand(() => NewSpreadsheet())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "grid.open",
            Title = "Open Spreadsheet",
            Description = "Open an existing spreadsheet",
            Glyph = "&#xE8E5;",
            Category = "File",
            Shortcut = "Ctrl+O",
            Command = new RelayCommand(() => OpenSpreadsheet())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "grid.save",
            Title = "Save",
            Description = "Save the current spreadsheet",
            Glyph = "&#xE74E;",
            Category = "File",
            Shortcut = "Ctrl+S",
            Command = new RelayCommand(() => SaveSpreadsheet())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "grid.insertrow",
            Title = "Insert Row",
            Description = "Insert a new row",
            Glyph = "&#xE73E;",
            Category = "Edit",
            Shortcut = "Ctrl+Shift+=",
            Command = new RelayCommand(() => InsertRow())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "grid.insertcol",
            Title = "Insert Column",
            Description = "Insert a new column",
            Glyph = "&#xE73E;",
            Category = "Edit",
            Shortcut = "Ctrl+Shift+=",
            Command = new RelayCommand(() => InsertColumn())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "grid.deletecells",
            Title = "Delete Cells",
            Description = "Delete selected cells",
            Glyph = "&#xE711;",
            Category = "Edit",
            Shortcut = "Ctrl+-",
            Command = new RelayCommand(() => DeleteCells())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "grid.find",
            Title = "Find",
            Description = "Find text in the spreadsheet",
            Glyph = "&#xE721;",
            Category = "Edit",
            Shortcut = "Ctrl+F",
            Command = new RelayCommand(() => ShowFind())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "grid.goto",
            Title = "Go To",
            Description = "Go to a specific cell",
            Glyph = "&#xE73E;",
            Category = "Edit",
            Shortcut = "Ctrl+G",
            Command = new RelayCommand(() => ShowGoTo())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "grid.sort",
            Title = "Sort",
            Description = "Sort selected data",
            Glyph = "&#xE73E;",
            Category = "Data",
            Command = new RelayCommand(() => ShowSort())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "grid.filter",
            Title = "Filter",
            Description = "Apply auto-filter",
            Glyph = "&#xE73E;",
            Category = "Data",
            Shortcut = "Ctrl+Shift+L",
            Command = new RelayCommand(() => ToggleFilter())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "grid.chart",
            Title = "Insert Chart",
            Description = "Create a chart from selection",
            Glyph = "&#xE73E;",
            Category = "Insert",
            Command = new RelayCommand(() => InsertChart())
        });
    }

    private void LoadContent()
    {
        var gridPage = new GridPage();
        Content = gridPage;
    }

    private void NewSpreadsheet() { /* Implementation */ }
    private void OpenSpreadsheet() { /* Implementation */ }
    private void SaveSpreadsheet() { /* Implementation */ }
    private void InsertRow() { /* Implementation */ }
    private void InsertColumn() { /* Implementation */ }
    private void DeleteCells() { /* Implementation */ }
    private void ShowFind() { /* Implementation */ }
    private void ShowGoTo() { /* Implementation */ }
    private void ShowSort() { /* Implementation */ }
    private void ToggleFilter() { /* Implementation */ }
    private void InsertChart() { /* Implementation */ }
}

/// <summary>
/// Grid document factory.
/// </summary>
public sealed class GridDocumentFactory : IDocumentFactory
{
    public string AppId => "Nova.Grid";
    public string[] SupportedExtensions => new[] { ".xlsx", ".csv" };

    public Task<DocumentModel> CreateNewAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DocumentModel>(new GridDocumentModel());
    }

    public Task<DocumentModel> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DocumentModel>(new GridDocumentModel(filePath));
    }

    public bool CanOpen(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(ext);
    }
}

/// <summary>
/// Grid document model.
/// </summary>
public sealed class GridDocumentModel : DocumentModel
{
    public GridDocumentModel() { }
    public GridDocumentModel(string filePath) { FilePath = filePath; }

    public override Task SaveAsync(CancellationToken cancellationToken = default)
    {
        OnSaved(true);
        return Task.CompletedTask;
    }

    public override Task SaveAsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        FilePath = filePath;
        OnSaved(true);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Grid page with spreadsheet.
/// </summary>
public sealed partial class GridPage : Page
{
    public GridPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto }, // Ribbon
                new RowDefinition { Height = GridLength.Auto }, // Formula bar
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, // Spreadsheet
                new RowDefinition { Height = GridLength.Auto }  // Status bar
            }
        };

        // Ribbon
        var ribbon = CreateRibbon();
        Grid.SetRow(ribbon, 0);
        grid.Children.Add(ribbon);

        // Formula bar
        var formulaBar = CreateFormulaBar();
        Grid.SetRow(formulaBar, 1);
        grid.Children.Add(formulaBar);

        // Spreadsheet area
        var spreadsheetGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto }, // Row headers
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } // Cells
            },
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto }, // Column headers
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } // Cells
            }
        };

        // Row headers
        var rowHeaders = new Border
        {
            Background = Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(0, 0, 1, 0),
            Width = 40,
            Child = new ListView
            {
                ItemsSource = Enumerable.Range(1, 1000).Select(i => i.ToString()),
                ItemTemplate = new DataTemplate(() =>
                {
                    var tb = new TextBlock { TextAlignment = TextAlignment.Center, Padding = new Thickness(4), FontSize = 11 };
                    tb.SetBinding(TextBlock.TextProperty, new Binding());
                    return tb;
                })
            }
        };
        Grid.SetRowSpan(rowHeaders, 2);
        Grid.SetRow(rowHeaders, 0);
        Grid.SetColumn(rowHeaders, 0);
        spreadsheetGrid.Children.Add(rowHeaders);

        // Column headers
        var colHeaders = new Border
        {
            Background = Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(0, 0, 0, 1),
            Height = 30,
            Child = new ListView
            {
                ItemsSource = Enumerable.Range(0, 26).Select(i => ((char)('A' + i)).ToString()),
                ItemsPanel = new ItemsPanelTemplate(() => new StackPanel { Orientation = Orientation.Horizontal }),
                ItemTemplate = new DataTemplate(() =>
                {
                    var tb = new TextBlock { TextAlignment = TextAlignment.Center, Padding = new Thickness(8, 4), FontSize = 11, Width = 80 };
                    tb.SetBinding(TextBlock.TextProperty, new Binding());
                    return tb;
                })
            }
        };
        Grid.SetRow(colHeaders, 0);
        Grid.SetColumn(colHeaders, 1);
        spreadsheetGrid.Children.Add(colHeaders);

        // Cell grid (placeholder - would use Win2D virtualized grid)
        var cellGrid = new Border
        {
            Background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
            Child = new GridView
            {
                ItemsSource = CreateCellData(100, 26),
                ItemsPanel = new ItemsPanelTemplate(() => new ItemsWrapGrid { Orientation = Orientation.Horizontal, ItemWidth = 80, ItemHeight = 24 }),
                ItemTemplate = new DataTemplate(() =>
                {
                    var tb = new TextBox { BorderThickness = new Thickness(0), Padding = new Thickness(4), FontSize = 11, Background = new SolidColorBrush(Colors.Transparent) };
                    tb.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath("Value"), Mode = BindingMode.TwoWay });
                    return tb;
                })
            }
        };
        Grid.SetRow(cellGrid, 1);
        Grid.SetColumn(cellGrid, 1);
        spreadsheetGrid.Children.Add(cellGrid);

        // Sheet tabs
        var sheetTabs = new TabView
        {
            TabItemsSource = new[] { "Sheet1", "Sheet2", "Sheet3" },
            IsAddTabButtonVisible = true,
            TabWidthMode = TabViewWidthMode.Equal,
            MinTabWidth = 100,
            MaxTabWidth = 150
        };
        Grid.SetRow(sheetTabs, 2);
        Grid.SetColumn(sheetTabs, 0);
        Grid.SetColumnSpan(sheetTabs, 2);
        spreadsheetGrid.Children.Add(sheetTabs);

        Grid.SetRow(spreadsheetGrid, 2);
        grid.Children.Add(spreadsheetGrid);

        // Status bar
        var statusBar = new GridStatusBar();
        Grid.SetRow(statusBar, 3);
        grid.Children.Add(statusBar);

        Content = grid;
    }

    private List<CellData> CreateCellData(int rows, int cols)
    {
        var data = new List<CellData>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                data.Add(new CellData { Row = r, Col = c, Value = "" });
            }
        }
        return data;
    }

    private Ribbon CreateRibbon()
    {
        var ribbon = new Ribbon { AccentColor = AppAccents.Grid };

        // Home tab
        var homeTab = new RibbonTab { Header = "Home", Glyph = "&#xE80F;" };
        homeTab.Groups.Add(new RibbonGroup
        {
            Header = "Clipboard",
            Controls =
            {
                new RibbonButton { Label = "Paste", Glyph = "&#xE77F;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Cut", Glyph = "&#xE8D6;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Copy", Glyph = "&#xE8C8;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Format Painter", Glyph = "&#xE780;", Command = new RelayCommand(() => { }) }
            }
        });
        homeTab.Groups.Add(new RibbonGroup
        {
            Header = "Font",
            Controls =
            {
                new RibbonComboBox { Label = "Font", Items = { "Segoe UI Variable", "Calibri", "Arial", "Consolas" }.Cast<object>().ToList(), Width = 150 },
                new RibbonComboBox { Label = "Size", Items = { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24 }.Cast<object>().ToList(), Width = 70 },
                new RibbonToggleButton { Label = "Bold", Glyph = "&#xE8D1;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Italic", Glyph = "&#xE8D2;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Underline", Glyph = "&#xE8D3;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Fill Color", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Font Color", Glyph = "&#xE8D0;", Command = new RelayCommand(() => { }) }
            }
        });
        homeTab.Groups.Add(new RibbonGroup
        {
            Header = "Alignment",
            Controls =
            {
                new RibbonToggleButton { Label = "Top Align", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Middle Align", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Bottom Align", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonSeparator(),
                new RibbonToggleButton { Label = "Align Left", Glyph = "&#xE8E3;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Center", Glyph = "&#xE8E4;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Align Right", Glyph = "&#xE8E5;", Command = new RelayCommand(() => { }) },
                new RibbonSeparator(),
                new RibbonButton { Label = "Wrap Text", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Merge & Center", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        homeTab.Groups.Add(new RibbonGroup
        {
            Header = "Number",
            Controls =
            {
                new RibbonComboBox { Label = "Format", Items = { "General", "Number", "Currency", "Percentage", "Date", "Time", "Text" }.Cast<object>().ToList(), Width = 130 },
                new RibbonButton { Label = "Increase Decimal", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Decrease Decimal", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        homeTab.Groups.Add(new RibbonGroup
        {
            Header = "Cells",
            Controls =
            {
                new RibbonButton { Label = "Insert", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Delete", Glyph = "&#xE711;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Format", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(homeTab);

        // Insert tab
        var insertTab = new RibbonTab { Header = "Insert", Glyph = "&#xE710;" };
        insertTab.Groups.Add(new RibbonGroup
        {
            Header = "Charts",
            Controls =
            {
                new RibbonButton { Label = "Column", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Line", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Pie", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Bar", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Area", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Scatter", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        insertTab.Groups.Add(new RibbonGroup
        {
            Header = "Tables",
            Controls =
            {
                new RibbonButton { Label = "Table", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "PivotTable", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(insertTab);

        // Formulas tab
        var formulasTab = new RibbonTab { Header = "Formulas", Glyph = "&#xE73E;" };
        formulasTab.Groups.Add(new RibbonGroup
        {
            Header = "Function Library",
            Controls =
            {
                new RibbonDropDownButton { Label = "AutoSum", Glyph = "&#xE73E;", Size = ControlSize.Large,
                    Items = {
                        new RibbonMenuItem { Label = "Sum", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "Average", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "Count", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "Min", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "Max", Glyph = "&#xE73E;" }
                    }
                },
                new RibbonDropDownButton { Label = "Financial", Glyph = "&#xE73E;", Items = {
                        new RibbonMenuItem { Label = "PMT", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "FV", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "PV", Glyph = "&#xE73E;" }
                    }
                },
                new RibbonDropDownButton { Label = "Logical", Glyph = "&#xE73E;", Items = {
                        new RibbonMenuItem { Label = "IF", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "AND", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "OR", Glyph = "&#xE73E;" }
                    }
                },
                new RibbonDropDownButton { Label = "Text", Glyph = "&#xE73E;", Items = {
                        new RibbonMenuItem { Label = "CONCAT", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "LEFT", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "RIGHT", Glyph = "&#xE73E;" }
                    }
                },
                new RibbonDropDownButton { Label = "Date & Time", Glyph = "&#xE73E;", Items = {
                        new RibbonMenuItem { Label = "TODAY", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "NOW", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "DATE", Glyph = "&#xE73E;" }
                    }
                },
                new RibbonDropDownButton { Label = "Lookup", Glyph = "&#xE73E;", Items = {
                        new RibbonMenuItem { Label = "VLOOKUP", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "XLOOKUP", Glyph = "&#xE73E;" },
                        new RibbonMenuItem { Label = "INDEX/MATCH", Glyph = "&#xE73E;" }
                    }
                }
            }
        });
        formulasTab.Groups.Add(new RibbonGroup
        {
            Header = "Formula Auditing",
            Controls =
            {
                new RibbonButton { Label = "Trace Precedents", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Trace Dependents", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Evaluate Formula", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        formulasTab.Groups.Add(new RibbonGroup
        {
            Header = "Calculation",
            Controls =
            {
                new RibbonButton { Label = "Calculate Now", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Automatic", IsChecked = true, Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(formulasTab);

        // Data tab
        var dataTab = new RibbonTab { Header = "Data", Glyph = "&#xE73E;" };
        dataTab.Groups.Add(new RibbonGroup
        {
            Header = "Sort & Filter",
            Controls =
            {
                new RibbonButton { Label = "Sort A-Z", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Sort Z-A", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Custom Sort", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Filter", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Clear", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(dataTab);

        // View tab
        var viewTab = new RibbonTab { Header = "View", Glyph = "&#xE727;" };
        viewTab.Groups.Add(new RibbonGroup
        {
            Header = "Workbook Views",
            Controls =
            {
                new RibbonToggleButton { Label = "Normal", Glyph = "&#xE73E;", IsChecked = true, Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Page Layout", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Page Break Preview", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        viewTab.Groups.Add(new RibbonGroup
        {
            Header = "Show",
            Controls =
            {
                new RibbonCheckBox { Label = "Gridlines", IsChecked = true, Command = new RelayCommand(() => { }) },
                new RibbonCheckBox { Label = "Headings", IsChecked = true, Command = new RelayCommand(() => { }) },
                new RibbonCheckBox { Label = "Formula Bar", IsChecked = true, Command = new RelayCommand(() => { }) }
            }
        });
        viewTab.Groups.Add(new RibbonGroup
        {
            Header = "Zoom",
            Controls =
            {
                new RibbonButton { Label = "Zoom", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "100%", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Fit Selection", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(viewTab);

        return ribbon;
    }

    private FrameworkElement CreateFormulaBar()
    {
        var border = new Border
        {
            Background = Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(8, 4),
            Child = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto }, // Name box
                    new ColumnDefinition { Width = GridLength.Auto }, // Function button
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } // Formula box
                }
            }
        };

        var nameBox = new ComboBox
        {
            Width = 100,
            PlaceholderText = "A1",
            FontSize = 13,
            ItemsSource = new[] { "A1", "B1", "C1" }
        };
        Grid.SetColumn(nameBox, 0);
        border.Child.Children.Add(nameBox);

        var fxButton = new Button
        {
            Content = new FontIcon { Glyph = "&#xE73E;", FontSize = 16 },
            Style = Application.Current.Resources["TransparentButtonStyle"] as Style,
            ToolTip = "Insert Function",
            Width = 36,
            Height = 32,
            Margin = new Thickness(8, 0, 8, 0)
        };
        Grid.SetColumn(fxButton, 1);
        border.Child.Children.Add(fxButton);

        var formulaBox = new TextBox
        {
            PlaceholderText = "Enter formula...",
            FontSize = 13,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
            Padding = new Thickness(8, 6),
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetColumn(formulaBox, 2);
        border.Child.Children.Add(formulaBox);

        return border;
    }
}

public sealed class CellData
{
    public int Row { get; set; }
    public int Col { get; set; }
    public string Value { get; set; } = "";
}