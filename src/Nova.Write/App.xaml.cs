using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Nova.Core;
using Nova.Formats;
using Nova.UI.Shared;
using Serilog;

namespace Nova.Write;

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
            "Nova.Write-.log");

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
        services.AddNovaCore("Nova.Write");

        // Formats
        services.AddNovaFormats();

        // UI Shared
        services.AddSingleton<ThemeService>();
        services.AddSingleton<ICommandPaletteService, CommandPaletteService>();
        services.AddSingleton<IRecentFilesService>(sp => sp.GetRequiredService<IRecentFilesService>());

        // Write-specific services
        services.AddSingleton<IDocumentFactory, WriteDocumentFactory>();

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
/// Main window for NOVA Write.
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
        Title = "NOVA Write";
        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
        AppWindow.Resize(new SizeInt32(1200, 800));
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }

    private void InitializeTitleBar()
    {
        var titleBar = new CustomTitleBar();
        titleBar.AppIcon = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/WriteIcon.png"));
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
        RegisterWriteCommands();
    }

    private void RegisterWriteCommands()
    {
        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "write.new",
            Title = "New Document",
            Description = "Create a new blank document",
            Glyph = "&#xE710;",
            Category = "File",
            Shortcut = "Ctrl+N",
            Command = new RelayCommand(() => NewDocument())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "write.open",
            Title = "Open Document",
            Description = "Open an existing document",
            Glyph = "&#xE8E5;",
            Category = "File",
            Shortcut = "Ctrl+O",
            Command = new RelayCommand(() => OpenDocument())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "write.save",
            Title = "Save",
            Description = "Save the current document",
            Glyph = "&#xE74E;",
            Category = "File",
            Shortcut = "Ctrl+S",
            Command = new RelayCommand(() => SaveDocument())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "write.saveas",
            Title = "Save As",
            Description = "Save the document with a new name",
            Glyph = "&#xE74E;",
            Category = "File",
            Shortcut = "Ctrl+Shift+S",
            Command = new RelayCommand(() => SaveAsDocument())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "write.exportpdf",
            Title = "Export to PDF",
            Description = "Export the document as PDF",
            Glyph = "&#xE74F;",
            Category = "File",
            Command = new RelayCommand(() => ExportToPdf())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "write.print",
            Title = "Print",
            Description = "Print the document",
            Glyph = "&#xE734;",
            Category = "File",
            Shortcut = "Ctrl+P",
            Command = new RelayCommand(() => PrintDocument())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "write.find",
            Title = "Find",
            Description = "Find text in the document",
            Glyph = "&#xE721;",
            Category = "Edit",
            Shortcut = "Ctrl+F",
            Command = new RelayCommand(() => ShowFind())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "write.replace",
            Title = "Replace",
            Description = "Find and replace text",
            Glyph = "&#xE73E;",
            Category = "Edit",
            Shortcut = "Ctrl+H",
            Command = new RelayCommand(() => ShowReplace())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "write.bold",
            Title = "Bold",
            Description = "Make text bold",
            Glyph = "&#xE8D1;",
            Category = "Format",
            Shortcut = "Ctrl+B",
            Command = new RelayCommand(() => ToggleBold())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "write.italic",
            Title = "Italic",
            Description = "Make text italic",
            Glyph = "&#xE8D2;",
            Category = "Format",
            Shortcut = "Ctrl+I",
            Command = new RelayCommand(() => ToggleItalic())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "write.underline",
            Title = "Underline",
            Description = "Underline text",
            Glyph = "&#xE8D3;",
            Category = "Format",
            Shortcut = "Ctrl+U",
            Command = new RelayCommand(() => ToggleUnderline())
        });
    }

    private void LoadContent()
    {
        var writePage = new WritePage();
        Content = writePage;
    }

    private void NewDocument() { /* Implementation */ }
    private void OpenDocument() { /* Implementation */ }
    private void SaveDocument() { /* Implementation */ }
    private void SaveAsDocument() { /* Implementation */ }
    private void ExportToPdf() { /* Implementation */ }
    private void PrintDocument() { /* Implementation */ }
    private void ShowFind() { /* Implementation */ }
    private void ShowReplace() { /* Implementation */ }
    private void ToggleBold() { /* Implementation */ }
    private void ToggleItalic() { /* Implementation */ }
    private void ToggleUnderline() { /* Implementation */ }
}

/// <summary>
/// Write document factory.
/// </summary>
public sealed class WriteDocumentFactory : IDocumentFactory
{
    public string AppId => "Nova.Write";
    public string[] SupportedExtensions => new[] { ".docx", ".rtf", ".txt" };

    public Task<DocumentModel> CreateNewAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DocumentModel>(new WriteDocumentModel());
    }

    public Task<DocumentModel> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DocumentModel>(new WriteDocumentModel(filePath));
    }

    public bool CanOpen(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(ext);
    }
}

/// <summary>
/// Write document model.
/// </summary>
public sealed class WriteDocumentModel : DocumentModel
{
    public WriteDocumentModel() { }
    public WriteDocumentModel(string filePath) { FilePath = filePath; }

    public override Task SaveAsync(CancellationToken cancellationToken = default)
    {
        // Implementation
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
/// Write page with editor.
/// </summary>
public sealed partial class WritePage : Page
{
    public WritePage()
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
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, // Editor
                new RowDefinition { Height = GridLength.Auto }  // Status bar
            }
        };

        // Ribbon
        var ribbon = CreateRibbon();
        Grid.SetRow(ribbon, 0);
        grid.Children.Add(ribbon);

        // Editor area
        var editorGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(300) }, // Navigation pane
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Document
                new ColumnDefinition { Width = new GridLength(200) }  // Review pane
            }
        };

        // Navigation pane
        var navPane = new Border
        {
            Background = Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Navigation", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(16, 16, 16, 8) },
                    new TextBlock { Text = "Headings", Margin = new Thickness(16, 0, 16, 8) },
                    new ListView { MaxHeight = 300 }
                }
            }
        };
        Grid.SetColumn(navPane, 0);
        editorGrid.Children.Add(navPane);

        // Document editor
        var docBorder = new Border
        {
            Background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
            Padding = new Thickness(48, 24),
            Child = new ScrollViewer
            {
                VerticalScrollMode = ScrollMode.Auto,
                HorizontalScrollMode = ScrollMode.Auto,
                Content = new RichEditBox
                {
                    MinWidth = 600,
                    MaxWidth = 800,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    PlaceholderText = "Start typing..."
                }
            }
        };
        Grid.SetColumn(docBorder, 1);
        editorGrid.Children.Add(docBorder);

        // Review pane
        var reviewPane = new Border
        {
            Background = Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1, 0, 0, 0),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Review", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(16, 16, 16, 8) },
                    new TextBlock { Text = "Comments", Margin = new Thickness(16, 0, 16, 8) },
                    new ListView { MaxHeight = 300 }
                }
            }
        };
        Grid.SetColumn(reviewPane, 2);
        editorGrid.Children.Add(reviewPane);

        Grid.SetRow(editorGrid, 1);
        grid.Children.Add(editorGrid);

        // Status bar
        var statusBar = new WriteStatusBar();
        Grid.SetRow(statusBar, 2);
        grid.Children.Add(statusBar);

        Content = grid;
    }

    private Ribbon CreateRibbon()
    {
        var ribbon = new Ribbon { AccentColor = AppAccents.Write };

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
                new RibbonComboBox { Label = "Font", Glyph = "&#xE8E1;", Items = { "Segoe UI Variable", "Calibri", "Arial", "Times New Roman", "Consolas" }, Width = 150 },
                new RibbonComboBox { Label = "Size", Glyph = "&#xE8E2;", Items = { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 36, 48, 72 }.Cast<object>().ToList(), Width = 70 },
                new RibbonButton { Label = "Grow Font", Glyph = "&#xE8D4;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Shrink Font", Glyph = "&#xE8D5;", Command = new RelayCommand(() => { }) },
                new RibbonSeparator(),
                new RibbonToggleButton { Label = "Bold", Glyph = "&#xE8D1;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Italic", Glyph = "&#xE8D2;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Underline", Glyph = "&#xE8D3;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Strikethrough", Glyph = "&#xE8D7;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Subscript", Glyph = "&#xE8D8;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Superscript", Glyph = "&#xE8D9;", Command = new RelayCommand(() => { }) },
                new RibbonSeparator(),
                new RibbonButton { Label = "Font Color", Glyph = "&#xE8D0;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Highlight", Glyph = "&#xE8CF;", Command = new RelayCommand(() => { }) }
            }
        });
        homeTab.Groups.Add(new RibbonGroup
        {
            Header = "Paragraph",
            Controls =
            {
                new RibbonToggleButton { Label = "Align Left", Glyph = "&#xE8E3;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Center", Glyph = "&#xE8E4;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Align Right", Glyph = "&#xE8E5;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Justify", Glyph = "&#xE8E6;", Command = new RelayCommand(() => { }) },
                new RibbonSeparator(),
                new RibbonButton { Label = "Bullets", Glyph = "&#xE8E7;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Numbering", Glyph = "&#xE8E8;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Decrease Indent", Glyph = "&#xE8E9;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Increase Indent", Glyph = "&#xE8EA;", Command = new RelayCommand(() => { }) },
                new RibbonSeparator(),
                new RibbonButton { Label = "Line Spacing", Glyph = "&#xE8EB;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Shading", Glyph = "&#xE8EC;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Borders", Glyph = "&#xE8ED;", Command = new RelayCommand(() => { }) }
            }
        });
        homeTab.Groups.Add(new RibbonGroup
        {
            Header = "Styles",
            Controls =
            {
                new RibbonGallery
                {
                    Label = "Styles",
                    Columns = 2,
                    Items =
                    {
                        new RibbonGalleryItem { Label = "Normal", Value = "Normal" },
                        new RibbonGalleryItem { Label = "Title", Value = "Title" },
                        new RibbonGalleryItem { Label = "Heading 1", Value = "Heading1" },
                        new RibbonGalleryItem { Label = "Heading 2", Value = "Heading2" },
                        new RibbonGalleryItem { Label = "Heading 3", Value = "Heading3" },
                        new RibbonGalleryItem { Label = "Quote", Value = "Quote" },
                        new RibbonGalleryItem { Label = "Code", Value = "Code" }
                    }
                }
            }
        });
        ribbon.Tabs.Add(homeTab);

        // Insert tab
        var insertTab = new RibbonTab { Header = "Insert", Glyph = "&#xE710;" };
        insertTab.Groups.Add(new RibbonGroup
        {
            Header = "Pages",
            Controls =
            {
                new RibbonButton { Label = "Cover Page", Glyph = "&#xE710;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Blank Page", Glyph = "&#xE711;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Page Break", Glyph = "&#xE712;", Command = new RelayCommand(() => { }) }
            }
        });
        insertTab.Groups.Add(new RibbonGroup
        {
            Header = "Tables",
            Controls =
            {
                new RibbonButton { Label = "Table", Glyph = "&#xE713;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) }
            }
        });
        insertTab.Groups.Add(new RibbonGroup
        {
            Header = "Illustrations",
            Controls =
            {
                new RibbonButton { Label = "Pictures", Glyph = "&#xE714;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Shapes", Glyph = "&#xE715;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Icons", Glyph = "&#xE716;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Chart", Glyph = "&#xE717;", Command = new RelayCommand(() => { }) }
            }
        });
        insertTab.Groups.Add(new RibbonGroup
        {
            Header = "Links",
            Controls =
            {
                new RibbonButton { Label = "Link", Glyph = "&#xE718;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Bookmark", Glyph = "&#xE719;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Cross-reference", Glyph = "&#xE71A;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(insertTab);

        // Layout tab
        var layoutTab = new RibbonTab { Header = "Layout", Glyph = "&#xE71B;" };
        layoutTab.Groups.Add(new RibbonGroup
        {
            Header = "Page Setup",
            Controls =
            {
                new RibbonButton { Label = "Margins", Glyph = "&#xE71C;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Orientation", Glyph = "&#xE71D;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Size", Glyph = "&#xE71E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Columns", Glyph = "&#xE71F;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Breaks", Glyph = "&#xE720;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(layoutTab);

        // References tab
        var refTab = new RibbonTab { Header = "References", Glyph = "&#xE721;" };
        refTab.Groups.Add(new RibbonGroup
        {
            Header = "Table of Contents",
            Controls =
            {
                new RibbonButton { Label = "Table of Contents", Glyph = "&#xE722;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(refTab);

        // Review tab
        var reviewTab = new RibbonTab { Header = "Review", Glyph = "&#xE723;" };
        reviewTab.Groups.Add(new RibbonGroup
        {
            Header = "Proofing",
            Controls =
            {
                new RibbonButton { Label = "Spelling & Grammar", Glyph = "&#xE724;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Thesaurus", Glyph = "&#xE725;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Word Count", Glyph = "&#xE726;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(reviewTab);

        // View tab
        var viewTab = new RibbonTab { Header = "View", Glyph = "&#xE727;" };
        viewTab.Groups.Add(new RibbonGroup
        {
            Header = "Views",
            Controls =
            {
                new RibbonToggleButton { Label = "Print Layout", Glyph = "&#xE728;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Web Layout", Glyph = "&#xE729;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Focus", Glyph = "&#xE72A;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Read Mode", Glyph = "&#xE72B;", Command = new RelayCommand(() => { }) }
            }
        });
        viewTab.Groups.Add(new RibbonGroup
        {
            Header = "Show",
            Controls =
            {
                new RibbonCheckBox { Label = "Ruler", IsChecked = true, Command = new RelayCommand(() => { }) },
                new RibbonCheckBox { Label = "Gridlines", Command = new RelayCommand(() => { }) },
                new RibbonCheckBox { Label = "Navigation Pane", IsChecked = true, Command = new RelayCommand(() => { }) }
            }
        });
        viewTab.Groups.Add(new RibbonGroup
        {
            Header = "Zoom",
            Controls =
            {
                new RibbonButton { Label = "Zoom", Glyph = "&#xE72C;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "100%", Glyph = "&#xE72D;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "One Page", Glyph = "&#xE72E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Two Pages", Glyph = "&#xE72F;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Page Width", Glyph = "&#xE730;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(viewTab);

        return ribbon;
    }
}