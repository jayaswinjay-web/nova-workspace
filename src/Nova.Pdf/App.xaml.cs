using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Nova.Core;
using Nova.Formats;
using Nova.UI.Shared;
using Serilog;

namespace Nova.Pdf;

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
            "Nova.Pdf-.log");

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
        services.AddNovaCore("Nova.Pdf");

        // Formats
        services.AddNovaFormats();

        // UI Shared
        services.AddSingleton<ThemeService>();
        services.AddSingleton<ICommandPaletteService, CommandPaletteService>();
        services.AddSingleton<IRecentFilesService>(sp => sp.GetRequiredService<IRecentFilesService>());

        // PDF-specific services
        services.AddSingleton<IDocumentFactory, PdfDocumentFactory>();

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
/// Main window for NOVA PDF.
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
        Title = "NOVA PDF";
        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
        AppWindow.Resize(new SizeInt32(1200, 800));
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }

    private void InitializeTitleBar()
    {
        var titleBar = new CustomTitleBar();
        titleBar.AppIcon = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/PdfIcon.png"));
        titleBar.DocumentTitle = "Untitled.pdf";
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
        RegisterPdfCommands();
    }

    private void RegisterPdfCommands()
    {
        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "pdf.open",
            Title = "Open PDF",
            Description = "Open a PDF file",
            Glyph = "&#xE8E5;",
            Category = "File",
            Shortcut = "Ctrl+O",
            Command = new RelayCommand(() => OpenPdf())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "pdf.save",
            Title = "Save",
            Description = "Save the PDF",
            Glyph = "&#xE74E;",
            Category = "File",
            Shortcut = "Ctrl+S",
            Command = new RelayCommand(() => SavePdf())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "pdf.saveas",
            Title = "Save As",
            Description = "Save the PDF with a new name",
            Glyph = "&#xE74E;",
            Category = "File",
            Shortcut = "Ctrl+Shift+S",
            Command = new RelayCommand(() => SaveAsPdf())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "pdf.print",
            Title = "Print",
            Description = "Print the PDF",
            Glyph = "&#xE734;",
            Category = "File",
            Shortcut = "Ctrl+P",
            Command = new RelayCommand(() => PrintPdf())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "pdf.find",
            Title = "Find",
            Description = "Find text in the PDF",
            Glyph = "&#xE721;",
            Category = "Edit",
            Shortcut = "Ctrl+F",
            Command = new RelayCommand(() => ShowFind())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "pdf.zoomIn",
            Title = "Zoom In",
            Description = "Zoom in",
            Glyph = "&#xE70D;",
            Category = "View",
            Shortcut = "Ctrl+=",
            Command = new RelayCommand(() => ZoomIn())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "pdf.zoomOut",
            Title = "Zoom Out",
            Description = "Zoom out",
            Glyph = "&#xE70E;",
            Category = "View",
            Shortcut = "Ctrl+-",
            Command = new RelayCommand(() => ZoomOut())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "pdf.fitWidth",
            Title = "Fit Width",
            Description = "Fit page to width",
            Glyph = "&#xE74A;",
            Category = "View",
            Command = new RelayCommand(() => FitWidth())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "pdf.fitPage",
            Title = "Fit Page",
            Description = "Fit entire page",
            Glyph = "&#xE749;",
            Category = "View",
            Command = new RelayCommand(() => FitPage())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "pdf.rotateLeft",
            Title = "Rotate Left",
            Description = "Rotate view left",
            Glyph = "&#xE73E;",
            Category = "View",
            Command = new RelayCommand(() => RotateLeft())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "pdf.rotateRight",
            Title = "Rotate Right",
            Description = "Rotate view right",
            Glyph = "&#xE73E;",
            Category = "View",
            Command = new RelayCommand(() => RotateRight())
        });
    }

    private void LoadContent()
    {
        var pdfPage = new PdfPage();
        Content = pdfPage;
    }

    private void OpenPdf() { /* Implementation */ }
    private void SavePdf() { /* Implementation */ }
    private void SaveAsPdf() { /* Implementation */ }
    private void PrintPdf() { /* Implementation */ }
    private void ShowFind() { /* Implementation */ }
    private void ZoomIn() { /* Implementation */ }
    private void ZoomOut() { /* Implementation */ }
    private void FitWidth() { /* Implementation */ }
    private void FitPage() { /* Implementation */ }
    private void RotateLeft() { /* Implementation */ }
    private void RotateRight() { /* Implementation */ }
}

/// <summary>
/// PDF document factory.
/// </summary>
public sealed class PdfDocumentFactory : IDocumentFactory
{
    public string AppId => "Nova.Pdf";
    public string[] SupportedExtensions => new[] { ".pdf" };

    public Task<DocumentModel> CreateNewAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DocumentModel>(new PdfDocumentModel());
    }

    public Task<DocumentModel> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DocumentModel>(new PdfDocumentModel(filePath));
    }

    public bool CanOpen(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(ext);
    }
}

/// <summary>
/// PDF document model.
/// </summary>
public sealed class PdfDocumentModel : DocumentModel
{
    public PdfDocumentModel() { }
    public PdfDocumentModel(string filePath) { FilePath = filePath; }

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
/// PDF page with viewer.
/// </summary>
public sealed partial class PdfPage : Page
{
    public PdfPage()
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
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } // Viewer
            }
        };

        // Ribbon
        var ribbon = CreateRibbon();
        Grid.SetRow(ribbon, 0);
        grid.Children.Add(ribbon);

        // Viewer area
        var viewerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(200) }, // Sidebar
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } // Page view
            }
        };

        // Sidebar
        var sidebar = new Border
        {
            Background = Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                },
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 4,
                        Margin = new Thickness(8),
                        Children =
                        {
                            new RadioButton { Content = "Thumbnails", GroupName = "Sidebar", IsChecked = true, Style = Application.Current.Resources["RadioButtonChipStyle"] as Style },
                            new RadioButton { Content = "Bookmarks", GroupName = "Sidebar", Style = Application.Current.Resources["RadioButtonChipStyle"] as Style },
                            new RadioButton { Content = "Annotations", GroupName = "Sidebar", Style = Application.Current.Resources["RadioButtonChipStyle"] as Style },
                            new RadioButton { Content = "Search", GroupName = "Sidebar", Style = Application.Current.Resources["RadioButtonChipStyle"] as Style }
                        }
                    },
                    new ListView
                    {
                        ItemsSource = Enumerable.Range(1, 10).Select(i => new PageThumb { Number = i }),
                        ItemTemplate = new DataTemplate(() =>
                        {
                            var border = new Border
                            {
                                BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                                BorderThickness = new Thickness(1),
                                CornerRadius = new CornerRadius(4),
                                Margin = new Thickness(8),
                                Background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
                                Child = new Grid
                                {
                                    RowDefinitions =
                                    {
                                        new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                                        new RowDefinition { Height = GridLength.Auto }
                                    },
                                    Children =
                                    {
                                        new Border
                                        {
                                            Background = Application.Current.Resources["ControlFillColorSecondaryBrush"],
                                            Child = new TextBlock { Text = "Page", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = Application.Current.Resources["TextFillColorTertiaryBrush"] }
                                        },
                                        new TextBlock { Text = "Page {Binding Number}", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Medium, Margin = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Center }
                                    }
                                }
                            };
                            return border;
                        })
                    }
                }
            }
        };
        Grid.SetColumn(sidebar, 0);
        viewerGrid.Children.Add(sidebar);

        // Page view
        var pageViewBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240)),
            Margin = new Thickness(16),
            CornerRadius = new CornerRadius(4),
            BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            Child = new ScrollViewer
            {
                VerticalScrollMode = ScrollMode.Auto,
                HorizontalScrollMode = ScrollMode.Auto,
                ZoomMode = ZoomMode.Enabled,
                MinZoomFactor = 0.1f,
                MaxZoomFactor = 10.0f,
                Content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 16,
                    Children = Enumerable.Range(1, 5).Select(i =>
                    {
                        return new Border
                        {
                            Background = new SolidColorBrush(Colors.White),
                            BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(2),
                            Width = 793,
                            Height = 1122,
                            Child = new TextBlock { Text = $"Page {i}", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = Application.Current.Resources["TextFillColorTertiaryBrush"], FontSize = 24 }
                        };
                    }).ToArray()
                }
            }
        };
        Grid.SetColumn(pageViewBorder, 1);
        viewerGrid.Children.Add(pageViewBorder);

        Grid.SetRow(viewerGrid, 1);
        grid.Children.Add(viewerGrid);

        Content = grid;
    }

    private Ribbon CreateRibbon()
    {
        var ribbon = new Ribbon { AccentColor = AppAccents.Pdf };

        // Home tab
        var homeTab = new RibbonTab { Header = "Home", Glyph = "&#xE80F;" };
        homeTab.Groups.Add(new RibbonGroup
        {
            Header = "Clipboard",
            Controls =
            {
                new RibbonButton { Label = "Copy", Glyph = "&#xE8C8;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Select", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Snapshot", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        homeTab.Groups.Add(new RibbonGroup
        {
            Header = "Tools",
            Controls =
            {
                new RibbonToggleButton { Label = "Select Tool", Glyph = "&#xE73E;", IsChecked = true, Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Hand Tool", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Marquee Zoom", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonSeparator(),
                new RibbonToggleButton { Label = "Highlight Text", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Underline Text", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Strikethrough Text", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonSeparator(),
                new RibbonToggleButton { Label = "Add Sticky Note", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Add Text Box", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Draw Freehand", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        homeTab.Groups.Add(new RibbonGroup
        {
            Header = "Find",
            Controls =
            {
                new RibbonButton { Label = "Find", Glyph = "&#xE721;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(homeTab);

        // View tab
        var viewTab = new RibbonTab { Header = "View", Glyph = "&#xE727;" };
        viewTab.Groups.Add(new RibbonGroup
        {
            Header = "Page Display",
            Controls =
            {
                new RibbonToggleButton { Label = "Single Page", Glyph = "&#xE73E;", IsChecked = true, Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Continuous", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Two Page", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Two Page Continuous", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        viewTab.Groups.Add(new RibbonGroup
        {
            Header = "Zoom",
            Controls =
            {
                new RibbonButton { Label = "Zoom In", Glyph = "&#xE70D;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Zoom Out", Glyph = "&#xE70E;", Command = new RelayCommand(() => { }) },
                new RibbonSeparator(),
                new RibbonButton { Label = "Fit Page", Glyph = "&#xE749;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Fit Width", Glyph = "&#xE74A;", Command = new RelayCommand(() => { }) },
                new RibbonSeparator(),
                new RibbonComboBox { Label = "Zoom", Items = { "50%", "75%", "100%", "125%", "150%", "200%", "300%", "400%" }.Cast<object>().ToList(), Width = 100 }
            }
        });
        viewTab.Groups.Add(new RibbonGroup
        {
            Header = "Rotate",
            Controls =
            {
                new RibbonButton { Label = "Rotate Left", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Rotate Right", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        viewTab.Groups.Add(new RibbonGroup
        {
            Header = "Show/Hide",
            Controls =
            {
                new RibbonCheckBox { Label = "Sidebar", IsChecked = true, Command = new RelayCommand(() => { }) },
                new RibbonCheckBox { Label = "Toolbar", IsChecked = true, Command = new RelayCommand(() => { }) },
                new RibbonCheckBox { Label = "Status Bar", IsChecked = true, Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(viewTab);

        // Organize tab
        var organizeTab = new RibbonTab { Header = "Organize", Glyph = "&#xE73E;" };
        organizeTab.Groups.Add(new RibbonGroup
        {
            Header = "Pages",
            Controls =
            {
                new RibbonButton { Label = "Insert", Glyph = "&#xE710;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Replace", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Extract", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Delete", Glyph = "&#xE711;", Command = new RelayCommand(() => { }) },
                new RibbonSeparator(),
                new RibbonButton { Label = "Rotate", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Crop", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonSeparator(),
                new RibbonButton { Label = "Split", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Merge", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(organizeTab);

        // Tools tab
        var toolsTab = new RibbonTab { Header = "Tools", Glyph = "&#xE73E;" };
        toolsTab.Groups.Add(new RibbonGroup
        {
            Header = "Forms",
            Controls =
            {
                new RibbonButton { Label = "Prepare Form", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Fill & Sign", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        toolsTab.Groups.Add(new RibbonGroup
        {
            Header = "Protect",
            Controls =
            {
                new RibbonButton { Label = "Protect", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Redact", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        toolsTab.Groups.Add(new RibbonGroup
        {
            Header = "Convert",
            Controls =
            {
                new RibbonButton { Label = "Export To Image", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Create PDF", Glyph = "&#xE710;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Combine Files", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(toolsTab);

        return ribbon;
    }
}

public sealed class PageThumb
{
    public int Number { get; set; }
}