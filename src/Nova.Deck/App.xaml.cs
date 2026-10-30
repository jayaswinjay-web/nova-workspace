using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Nova.Core;
using Nova.Formats;
using Nova.UI.Shared;
using Serilog;

namespace Nova.Deck;

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
            "Nova.Deck-.log");

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
        services.AddNovaCore("Nova.Deck");

        // Formats
        services.AddNovaFormats();

        // UI Shared
        services.AddSingleton<ThemeService>();
        services.AddSingleton<ICommandPaletteService, CommandPaletteService>();
        services.AddSingleton<IRecentFilesService>(sp => sp.GetRequiredService<IRecentFilesService>());

        // Deck-specific services
        services.AddSingleton<IDocumentFactory, DeckDocumentFactory>();

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
/// Main window for NOVA Deck.
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
        Title = "NOVA Deck";
        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
        AppWindow.Resize(new SizeInt32(1366, 768));
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }

    private void InitializeTitleBar()
    {
        var titleBar = new CustomTitleBar();
        titleBar.AppIcon = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/DeckIcon.png"));
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
        RegisterDeckCommands();
    }

    private void RegisterDeckCommands()
    {
        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "deck.new",
            Title = "New Presentation",
            Description = "Create a new blank presentation",
            Glyph = "&#xE710;",
            Category = "File",
            Shortcut = "Ctrl+N",
            Command = new RelayCommand(() => NewPresentation())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "deck.open",
            Title = "Open Presentation",
            Description = "Open an existing presentation",
            Glyph = "&#xE8E5;",
            Category = "File",
            Shortcut = "Ctrl+O",
            Command = new RelayCommand(() => OpenPresentation())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "deck.save",
            Title = "Save",
            Description = "Save the current presentation",
            Glyph = "&#xE74E;",
            Category = "File",
            Shortcut = "Ctrl+S",
            Command = new RelayCommand(() => SavePresentation())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "deck.newslide",
            Title = "New Slide",
            Description = "Add a new slide",
            Glyph = "&#xE710;",
            Category = "Slides",
            Shortcut = "Ctrl+M",
            Command = new RelayCommand(() => NewSlide())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "deck.duplicateslide",
            Title = "Duplicate Slide",
            Description = "Duplicate the current slide",
            Glyph = "&#xE73E;",
            Category = "Slides",
            Shortcut = "Ctrl+D",
            Command = new RelayCommand(() => DuplicateSlide())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "deck.slideshow",
            Title = "Start Slide Show",
            Description = "Start the slide show from beginning",
            Glyph = "&#xE73E;",
            Category = "Slide Show",
            Shortcut = "F5",
            Command = new RelayCommand(() => StartSlideShow())
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "deck.slideshowcurrent",
            Title = "Start from Current Slide",
            Description = "Start slide show from current slide",
            Glyph = "&#xE73E;",
            Category = "Slide Show",
            Shortcut = "Shift+F5",
            Command = new RelayCommand(() => StartSlideShowFromCurrent())
        });
    }

    private void LoadContent()
    {
        var deckPage = new DeckPage();
        Content = deckPage;
    }

    private void NewPresentation() { /* Implementation */ }
    private void OpenPresentation() { /* Implementation */ }
    private void SavePresentation() { /* Implementation */ }
    private void NewSlide() { /* Implementation */ }
    private void DuplicateSlide() { /* Implementation */ }
    private void StartSlideShow() { /* Implementation */ }
    private void StartSlideShowFromCurrent() { /* Implementation */ }
}

/// <summary>
/// Deck document factory.
/// </summary>
public sealed class DeckDocumentFactory : IDocumentFactory
{
    public string AppId => "Nova.Deck";
    public string[] SupportedExtensions => new[] { ".pptx" };

    public Task<DocumentModel> CreateNewAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DocumentModel>(new DeckDocumentModel());
    }

    public Task<DocumentModel> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DocumentModel>(new DeckDocumentModel(filePath));
    }

    public bool CanOpen(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(ext);
    }
}

/// <summary>
/// Deck document model.
/// </summary>
public sealed class DeckDocumentModel : DocumentModel
{
    public DeckDocumentModel() { }
    public DeckDocumentModel(string filePath) { FilePath = filePath; }

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
/// Deck page with presentation editor.
/// </summary>
public sealed partial class DeckPage : Page
{
    public DeckPage()
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
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } // Editor
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
                new ColumnDefinition { Width = new GridLength(240) }, // Thumbnail pane
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Slide canvas
                new ColumnDefinition { Width = new GridLength(280) }  // Notes/Properties pane
            }
        };

        // Thumbnail pane
        var thumbPane = new Border
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
                        Spacing = 8,
                        Margin = new Thickness(8),
                        Children =
                        {
                            new TextBlock { Text = "Slides", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center },
                            new Button { Content = "New Slide", Style = Application.Current.Resources["AccentButtonStyle"] as Style }
                        }
                    },
                    new ListView
                    {
                        ItemsSource = Enumerable.Range(1, 5).Select(i => new SlideThumb { Number = i, Title = $"Slide {i}" }),
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
                                            Child = new TextBlock { Text = "Slide Content", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = Application.Current.Resources["TextFillColorTertiaryBrush"] }
                                        },
                                        new TextBlock { Text = "{Binding Title}", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Medium, Margin = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Center }
                                    }
                                }
                            };
                            return border;
                        })
                    }
                }
            }
        };
        Grid.SetRow(thumbPane, 0);
        Grid.SetRowSpan(thumbPane, 2);
        Grid.SetColumn(thumbPane, 0);
        editorGrid.Children.Add(thumbPane);

        // Slide canvas
        var canvasBorder = new Border
        {
            Background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
            Margin = new Thickness(24),
            CornerRadius = new CornerRadius(4),
            BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            Child = new Canvas
            {
                Background = new SolidColorBrush(Colors.White),
                Width = 960,
                Height = 540,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(canvasBorder, 1);
        editorGrid.Children.Add(canvasBorder);

        // Notes/Properties pane
        var notesPane = new Border
        {
            Background = Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1, 0, 0, 0),
            Child = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                },
                Children =
                {
                    new TextBlock { Text = "Notes", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(16, 16, 16, 8) },
                    new TextBox
                    {
                        AcceptsReturn = true,
                        TextWrapping = TextWrapping.Wrap,
                        PlaceholderText = "Add notes...",
                        Margin = new Thickness(16, 0, 16, 16),
                        FontSize = 12
                    },
                    new TextBlock { Text = "Layout", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(16, 16, 16, 8) },
                    new ComboBox
                    {
                        ItemsSource = new[] { "Title", "Title and Content", "Two Content", "Section Header", "Blank", "Picture with Caption" },
                        SelectedIndex = 0,
                        Margin = new Thickness(16, 0, 16, 16)
                    }
                }
            }
        };
        Grid.SetColumn(notesPane, 2);
        editorGrid.Children.Add(notesPane);

        Grid.SetRow(editorGrid, 1);
        grid.Children.Add(editorGrid);

        Content = grid;
    }

    private Ribbon CreateRibbon()
    {
        var ribbon = new Ribbon { AccentColor = AppAccents.Deck };

        // Home tab
        var homeTab = new RibbonTab { Header = "Home", Glyph = "&#xE80F;" };
        homeTab.Groups.Add(new RibbonGroup
        {
            Header = "Slides",
            Controls =
            {
                new RibbonButton { Label = "New Slide", Glyph = "&#xE710;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Layout", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Reset", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Delete", Glyph = "&#xE711;", Command = new RelayCommand(() => { }) }
            }
        });
        homeTab.Groups.Add(new RibbonGroup
        {
            Header = "Font",
            Controls =
            {
                new RibbonComboBox { Label = "Font", Items = { "Segoe UI Variable", "Calibri", "Arial", "Georgia" }.Cast<object>().ToList(), Width = 150 },
                new RibbonComboBox { Label = "Size", Items = { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 36, 40, 44, 48, 54, 60, 72, 96 }.Cast<object>().ToList(), Width = 70 },
                new RibbonToggleButton { Label = "Bold", Glyph = "&#xE8D1;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Italic", Glyph = "&#xE8D2;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Underline", Glyph = "&#xE8D3;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Font Color", Glyph = "&#xE8D0;", Command = new RelayCommand(() => { }) }
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
                new RibbonButton { Label = "Increase Indent", Glyph = "&#xE8EA;", Command = new RelayCommand(() => { }) }
            }
        });
        homeTab.Groups.Add(new RibbonGroup
        {
            Header = "Drawing",
            Controls =
            {
                new RibbonButton { Label = "Shapes", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Text Box", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(homeTab);

        // Insert tab
        var insertTab = new RibbonTab { Header = "Insert", Glyph = "&#xE710;" };
        insertTab.Groups.Add(new RibbonGroup
        {
            Header = "Slides",
            Controls =
            {
                new RibbonButton { Label = "New Slide", Glyph = "&#xE710;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Reuse Slides", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        insertTab.Groups.Add(new RibbonGroup
        {
            Header = "Tables",
            Controls =
            {
                new RibbonButton { Label = "Table", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) }
            }
        });
        insertTab.Groups.Add(new RibbonGroup
        {
            Header = "Images",
            Controls =
            {
                new RibbonButton { Label = "Pictures", Glyph = "&#xE714;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Online Pictures", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Screenshot", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        insertTab.Groups.Add(new RibbonGroup
        {
            Header = "Illustrations",
            Controls =
            {
                new RibbonButton { Label = "Shapes", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Icons", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "SmartArt", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Chart", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        insertTab.Groups.Add(new RibbonGroup
        {
            Header = "Links",
            Controls =
            {
                new RibbonButton { Label = "Link", Glyph = "&#xE718;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Action", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        insertTab.Groups.Add(new RibbonGroup
        {
            Header = "Text",
            Controls =
            {
                new RibbonButton { Label = "Text Box", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Header & Footer", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "WordArt", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Date & Time", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Slide Number", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        insertTab.Groups.Add(new RibbonGroup
        {
            Header = "Media",
            Controls =
            {
                new RibbonButton { Label = "Video", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Audio", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Screen Recording", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(insertTab);

        // Design tab
        var designTab = new RibbonTab { Header = "Design", Glyph = "&#xE73E;" };
        designTab.Groups.Add(new RibbonGroup
        {
            Header = "Themes",
            Controls =
            {
                new RibbonGallery
                {
                    Label = "Themes",
                    Columns = 3,
                    Items =
                    {
                        new RibbonGalleryItem { Label = "Office", Value = "Office" },
                        new RibbonGalleryItem { Label = "Futuristic Glass", Value = "FuturisticGlass" },
                        new RibbonGalleryItem { Label = "Dark Glass", Value = "DarkGlass" },
                        new RibbonGalleryItem { Label = "Minimal Light", Value = "MinimalLight" },
                        new RibbonGalleryItem { Label = "Minimal Dark", Value = "MinimalDark" },
                        new RibbonGalleryItem { Label = "High Contrast", Value = "HighContrast" }
                    }
                },
                new RibbonButton { Label = "Variants", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        designTab.Groups.Add(new RibbonGroup
        {
            Header = "Slide Size",
            Controls =
            {
                new RibbonButton { Label = "Slide Size", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) }
            }
        });
        designTab.Groups.Add(new RibbonGroup
        {
            Header = "Background",
            Controls =
            {
                new RibbonButton { Label = "Format Background", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(designTab);

        // Transitions tab
        var transTab = new RibbonTab { Header = "Transitions", Glyph = "&#xE73E;" };
        transTab.Groups.Add(new RibbonGroup
        {
            Header = "Transition to This Slide",
            Controls =
            {
                new RibbonGallery
                {
                    Label = "Transitions",
                    Columns = 4,
                    Items =
                    {
                        new RibbonGalleryItem { Label = "None", Value = "None" },
                        new RibbonGalleryItem { Label = "Fade", Value = "Fade" },
                        new RibbonGalleryItem { Label = "Push", Value = "Push" },
                        new RibbonGalleryItem { Label = "Wipe", Value = "Wipe" },
                        new RibbonGalleryItem { Label = "Zoom", Value = "Zoom" },
                        new RibbonGalleryItem { Label = "Morph", Value = "Morph" }
                    }
                }
            }
        });
        transTab.Groups.Add(new RibbonGroup
        {
            Header = "Timing",
            Controls =
            {
                new RibbonCheckBox { Label = "On Mouse Click", IsChecked = true, Command = new RelayCommand(() => { }) },
                new RibbonCheckBox { Label = "After", IsChecked = false, Command = new RelayCommand(() => { }) },
                new RibbonTextBox { Label = "Duration:", PlaceholderText = "00:00", Width = 80 }
            }
        });
        ribbon.Tabs.Add(transTab);

        // Animations tab
        var animTab = new RibbonTab { Header = "Animations", Glyph = "&#xE73E;" };
        animTab.Groups.Add(new RibbonGroup
        {
            Header = "Animation",
            Controls =
            {
                new RibbonGallery
                {
                    Label = "Animations",
                    Columns = 3,
                    Items =
                    {
                        new RibbonGalleryItem { Label = "None", Value = "None" },
                        new RibbonGalleryItem { Label = "Fade", Value = "Fade" },
                        new RibbonGalleryItem { Label = "Fly In", Value = "FlyIn" },
                        new RibbonGalleryItem { Label = "Zoom", Value = "Zoom" },
                        new RibbonGalleryItem { Label = "Appear", Value = "Appear" },
                        new RibbonGalleryItem { Label = "Pulse", Value = "Pulse" }
                    }
                }
            }
        });
        animTab.Groups.Add(new RibbonGroup
        {
            Header = "Timing",
            Controls =
            {
                new RibbonComboBox { Label = "Start:", Items = { "On Click", "With Previous", "After Previous" }.Cast<object>().ToList(), Width = 130 },
                new RibbonTextBox { Label = "Duration:", PlaceholderText = "00:00", Width = 80 },
                new RibbonTextBox { Label = "Delay:", PlaceholderText = "00:00", Width = 80 }
            }
        });
        animTab.Groups.Add(new RibbonGroup
        {
            Header = "Advanced Animation",
            Controls =
            {
                new RibbonButton { Label = "Animation Pane", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Add Animation", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(animTab);

        // Slide Show tab
        var showTab = new RibbonTab { Header = "Slide Show", Glyph = "&#xE73E;" };
        showTab.Groups.Add(new RibbonGroup
        {
            Header = "Start Slide Show",
            Controls =
            {
                new RibbonButton { Label = "From Beginning", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "From Current Slide", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Presenter View", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        showTab.Groups.Add(new RibbonGroup
        {
            Header = "Set Up",
            Controls =
            {
                new RibbonButton { Label = "Set Up Slide Show", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Record Slide Show", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(showTab);

        // View tab
        var viewTab = new RibbonTab { Header = "View", Glyph = "&#xE727;" };
        viewTab.Groups.Add(new RibbonGroup
        {
            Header = "Presentation Views",
            Controls =
            {
                new RibbonToggleButton { Label = "Normal", Glyph = "&#xE73E;", IsChecked = true, Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Slide Sorter", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Reading View", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) },
                new RibbonToggleButton { Label = "Slide Master", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        viewTab.Groups.Add(new RibbonGroup
        {
            Header = "Show",
            Controls =
            {
                new RibbonCheckBox { Label = "Ruler", IsChecked = true, Command = new RelayCommand(() => { }) },
                new RibbonCheckBox { Label = "Gridlines", Command = new RelayCommand(() => { }) },
                new RibbonCheckBox { Label = "Guides", Command = new RelayCommand(() => { }) }
            }
        });
        viewTab.Groups.Add(new RibbonGroup
        {
            Header = "Zoom",
            Controls =
            {
                new RibbonButton { Label = "Zoom", Glyph = "&#xE73E;", Size = ControlSize.Large, Command = new RelayCommand(() => { }) },
                new RibbonButton { Label = "Fit to Window", Glyph = "&#xE73E;", Command = new RelayCommand(() => { }) }
            }
        });
        ribbon.Tabs.Add(viewTab);

        return ribbon;
    }
}

public sealed class SlideThumb
{
    public int Number { get; set; }
    public string Title { get; set; } = "";
}