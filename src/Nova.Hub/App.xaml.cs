using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Nova.Core;
using Nova.UI.Shared;
using Serilog;
using System.Diagnostics;

namespace Nova.Hub;

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
            "Nova.Hub-.log");

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
        services.AddNovaCore("Nova.Hub");

        // UI services
        services.AddSingleton<ThemeService>();
        services.AddSingleton<ICommandPaletteService, CommandPaletteService>();
        services.AddSingleton<IRecentFilesService>(sp => sp.GetRequiredService<IRecentFilesService>());

        Services = services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();

        // Set up theme
        var themeService = Services.GetRequiredService<ThemeService>();
        themeService.InitializeWindow(MainWindow);
    }

    protected override void OnWindowCreated(WindowCreatedEventArgs args)
    {
        base.OnWindowCreated(args);
    }
}

/// <summary>
/// Main window for NOVA Hub.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ThemeService _themeService;
    private readonly IRecentFilesService _recentFilesService;
    private readonly ICommandPaletteService _commandPaletteService;
    private readonly CommandPalette _commandPalette;
    private readonly CommandPaletteShortcutHandler _shortcutHandler;

    public MainWindow()
    {
        _themeService = App.Services.GetRequiredService<ThemeService>();
        _recentFilesService = App.Services.GetRequiredService<IRecentFilesService>();
        _commandPaletteService = App.Services.GetRequiredService<ICommandPaletteService>();

        InitializeComponent();
        InitializeTitleBar();
        InitializeCommandPalette();
        LoadContent();
    }

    private void InitializeComponent()
    {
        Title = "NOVA Hub";
        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };

        // Set minimum size
        AppWindow.Resize(new SizeInt32(1000, 700));
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }

    private void InitializeTitleBar()
    {
        var titleBar = new CustomTitleBar();
        titleBar.AppIcon = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/HubIcon.png"));
        titleBar.DocumentTitle = "NOVA Office";
        titleBar.SaveStatus = SaveStatus.Saved;
        titleBar.ShowSearchBox = false;
        titleBar.Initialize(this, _themeService);

        // Add to window
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

        // Register global commands
        RegisterGlobalCommands();
    }

    private void RegisterGlobalCommands()
    {
        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "nova.write.new",
            Title = "New Document (Write)",
            Description = "Create a new word processing document",
            Glyph = "&#xE8A5;",
            Category = "File",
            Shortcut = "Ctrl+N",
            Command = new RelayCommand(() => LaunchApp("Nova.Write"))
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "nova.grid.new",
            Title = "New Spreadsheet (Grid)",
            Description = "Create a new spreadsheet",
            Glyph = "&#xE94A;",
            Category = "File",
            Shortcut = "Ctrl+Shift+N",
            Command = new RelayCommand(() => LaunchApp("Nova.Grid"))
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "nova.deck.new",
            Title = "New Presentation (Deck)",
            Description = "Create a new presentation",
            Glyph = "&#xE7B4;",
            Category = "File",
            Command = new RelayCommand(() => LaunchApp("Nova.Deck"))
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "nova.pdf.new",
            Title = "Open PDF (PDF)",
            Description = "Open a PDF document",
            Glyph = "&#xE8A5;",
            Category = "File",
            Command = new RelayCommand(() => LaunchApp("Nova.Pdf"))
        });

        _commandPaletteService.RegisterCommand(new CommandPaletteItem
        {
            Id = "nova.settings",
            Title = "Settings",
            Description = "Open application settings",
            Glyph = "&#xE713;",
            Category = "Application",
            Shortcut = "Ctrl+,",
            Command = new RelayCommand(() => ShowSettings())
        });
    }

    private void LoadContent()
    {
        var hubPage = new HubPage(_recentFilesService);
        Content = hubPage;
    }

    private void LaunchApp(string appId)
    {
        try
        {
            var exeName = appId + ".exe";
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            var exePath = Path.Combine(currentDir, exeName);

            if (File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
            }
            else
            {
                // Try to find in installed location
                var installedPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft",
                    "WindowsApps",
                    exeName);
                if (File.Exists(installedPath))
                {
                    Process.Start(new ProcessStartInfo(installedPath) { UseShellExecute = true });
                }
            }
        }
        catch (Exception ex)
        {
            // Log error
        }
    }

    private void ShowSettings()
    {
        // Show settings dialog
    }
}

/// <summary>
/// Hub page with app launcher tiles and recent files.
/// </summary>
public sealed partial class HubPage : Page
{
    private readonly IRecentFilesService _recentFilesService;

    public HubPage(IRecentFilesService recentFilesService)
    {
        _recentFilesService = recentFilesService;
        InitializeComponent();
        LoadRecentFiles();
    }

    private void InitializeComponent()
    {
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollMode = ScrollMode.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(32)
        };

        var mainStack = new StackPanel { Spacing = 32 };

        // Header
        var headerStack = new StackPanel { Spacing = 8 };
        headerStack.Children.Add(new TextBlock
        {
            Text = "NOVA Office",
            FontSize = 48,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = Application.Current.Resources["TextFillColorPrimaryBrush"]
        });
        headerStack.Children.Add(new TextBlock
        {
            Text = "Your futuristic office suite",
            FontSize = 20,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });
        mainStack.Children.Add(headerStack);

        // App tiles
        mainStack.Children.Add(CreateSectionHeader("Applications", "Launch an app"));
        mainStack.Children.Add(CreateAppTiles());

        // Quick actions
        mainStack.Children.Add(CreateSectionHeader("Quick Actions", "Common tasks"));
        mainStack.Children.Add(CreateQuickActions());

        // Recent files
        mainStack.Children.Add(CreateSectionHeader("Recent Files", "Your recently opened documents"));
        mainStack.Children.Add(CreateRecentFilesSection());

        scrollViewer.Content = mainStack;
        Content = scrollViewer;
    }

    private FrameworkElement CreateSectionHeader(string title, string subtitle)
    {
        var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(new TextBlock { Text = title, FontSize = 24, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        stack.Children.Add(new TextBlock { Text = subtitle, FontSize = 14, Foreground = Application.Current.Resources["TextFillColorTertiaryBrush"], VerticalAlignment = VerticalAlignment.Bottom });
        return stack;
    }

    private FrameworkElement CreateAppTiles()
    {
        var wrapGrid = new ItemsWrapGrid { Orientation = Orientation.Horizontal, ItemWidth = 220, ItemHeight = 160 };
        var itemsControl = new ItemsControl { ItemsPanel = new ItemsPanelTemplate { VisualTree = wrapGrid } };

        var apps = new[]
        {
            new { Id = "Nova.Write", Name = "NOVA Write", Description = "Word Processor", Glyph = "&#xE8A5;", Accent = "#0078FF", Color = Colors.Blue },
            new { Id = "Nova.Grid", Name = "NOVA Grid", Description = "Spreadsheets", Glyph = "&#xE94A;", Accent = "#00B45A", Color = Colors.Green },
            new { Id = "Nova.Deck", Name = "NOVA Deck", Description = "Presentations", Glyph = "&#xE7B4;", Accent = "#FFA000", Color = Colors.Orange },
            new { Id = "Nova.Pdf", Name = "NOVA PDF", Description = "PDF Viewer & Editor", Glyph = "&#xE8A5;", Accent = "#DC143C", Color = Colors.Red },
        };

        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
        factory.SetValue(Border.BackgroundProperty, Application.Current.Resources["ControlFillColorDefaultBrush"]);
        factory.SetValue(Border.BorderBrushProperty, Application.Current.Resources["ControlStrokeColorDefaultBrush"]);
        factory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        factory.SetValue(Border.PaddingProperty, new Thickness(24));
        factory.SetValue(Border.MarginProperty, new Thickness(8));
        factory.SetValue(FrameworkElement.WidthProperty, 200.0);
        factory.SetValue(FrameworkElement.HeightProperty, 140.0);
        factory.SetValue(FrameworkElement.CursorProperty, Microsoft.UI.Xaml.Input.InputSystemCursor.Create(Microsoft.UI.Xaml.Input.InputSystemCursorType.Hand));

        var stack = new FrameworkElementFactory(typeof(StackPanel));
        stack.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
        stack.SetValue(StackPanel.SpacingProperty, 12);
        stack.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        stack.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);

        var iconBorder = new FrameworkElementFactory(typeof(Border));
        iconBorder.SetValue(Border.WidthProperty, 56.0);
        iconBorder.SetValue(Border.HeightProperty, 56.0);
        iconBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
        iconBorder.SetValue(Border.BackgroundProperty, new Binding { Path = new PropertyPath("Accent"), Converter = new StringToBrushConverter() });
        iconBorder.SetValue(Border.ChildProperty, new FrameworkElementFactory(typeof(FontIcon))
        {
            { FontIcon.GlyphProperty, new Binding { Path = new PropertyPath("Glyph") } },
            { FontIcon.FontSizeProperty, 28.0 },
            { FontIcon.ForegroundProperty, new SolidColorBrush(Colors.White) },
            { FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center },
            { FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center }
        });
        stack.AppendChild(iconBorder);

        var nameText = new FrameworkElementFactory(typeof(TextBlock));
        nameText.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Name") });
        nameText.SetValue(TextBlock.FontSizeProperty, 18.0);
        nameText.SetValue(TextBlock.FontWeightProperty, Microsoft.UI.Text.FontWeights.SemiBold);
        nameText.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextFillColorPrimaryBrush"]);
        nameText.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        stack.AppendChild(nameText);

        var descText = new FrameworkElementFactory(typeof(TextBlock));
        descText.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Description") });
        descText.SetValue(TextBlock.FontSizeProperty, 13.0);
        descText.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextFillColorSecondaryBrush"]);
        descText.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        stack.AppendChild(descText);

        factory.SetValue(Border.ChildProperty, stack);
        template.VisualTree = factory;
        itemsControl.ItemTemplate = template;
        itemsControl.ItemsSource = apps;

        itemsControl.ItemClick += (s, e) =>
        {
            if (e.ClickedItem is dynamic app)
            {
                LaunchApp(app.Id);
            }
        };
        itemsControl.IsItemClickEnabled = true;

        return itemsControl;
    }

    private void LaunchApp(string appId)
    {
        try
        {
            var exeName = appId + ".exe";
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            var exePath = Path.Combine(currentDir, exeName);

            if (File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
            }
        }
        catch { }
    }

    private FrameworkElement CreateQuickActions()
    {
        var wrapGrid = new ItemsWrapGrid { Orientation = Orientation.Horizontal, ItemWidth = 180, ItemHeight = 100 };
        var itemsControl = new ItemsControl { ItemsPanel = new ItemsPanelTemplate { VisualTree = wrapGrid } };

        var actions = new[]
        {
            new { Glyph = "&#xE710;", Title = "New Document", Action = "write" },
            new { Glyph = "&#xE94A;", Title = "New Spreadsheet", Action = "grid" },
            new { Glyph = "&#xE7B4;", Title = "New Presentation", Action = "deck" },
            new { Glyph = "&#xE8E5;", Title = "Open File", Action = "open" },
            new { Glyph = "&#xE74F;", Title = "Export PDF", Action = "export" },
            new { Glyph = "&#xE713;", Title = "Settings", Action = "settings" },
        };

        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Button));
        factory.SetValue(Button.StyleProperty, Application.Current.Resources["SecondaryButtonStyle"]);
        factory.SetValue(Button.WidthProperty, 160.0);
        factory.SetValue(Button.HeightProperty, 80.0);
        factory.SetValue(Button.MarginProperty, new Thickness(8));
        factory.SetValue(Button.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        factory.SetValue(Button.VerticalAlignmentProperty, VerticalAlignment.Stretch);

        var stack = new FrameworkElementFactory(typeof(StackPanel));
        stack.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
        stack.SetValue(StackPanel.SpacingProperty, 8);
        stack.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        stack.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);

        var icon = new FrameworkElementFactory(typeof(FontIcon));
        icon.SetValue(FontIcon.GlyphProperty, new Binding { Path = new PropertyPath("Glyph") });
        icon.SetValue(FontIcon.FontSizeProperty, 24.0);
        icon.SetValue(FontIcon.ForegroundProperty, Application.Current.Resources["SystemAccentColorBrush"]);
        stack.AppendChild(icon);

        var label = new FrameworkElementFactory(typeof(TextBlock));
        label.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Title") });
        label.SetValue(TextBlock.FontSizeProperty, 14.0);
        label.SetValue(TextBlock.FontWeightProperty, Microsoft.UI.Text.FontWeights.Medium);
        label.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextFillColorPrimaryBrush"]);
        label.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        stack.AppendChild(label);

        factory.SetValue(Button.ContentProperty, stack);
        template.VisualTree = factory;
        itemsControl.ItemTemplate = template;
        itemsControl.ItemsSource = actions;

        itemsControl.ItemClick += (s, e) =>
        {
            if (e.ClickedItem is dynamic action)
            {
                switch (action.Action)
                {
                    case "write": LaunchApp("Nova.Write"); break;
                    case "grid": LaunchApp("Nova.Grid"); break;
                    case "deck": LaunchApp("Nova.Deck"); break;
                    case "open": /* Open file dialog */ break;
                }
            }
        };
        itemsControl.IsItemClickEnabled = true;

        return itemsControl;
    }

    private FrameworkElement CreateRecentFilesSection()
    {
        var stack = new StackPanel { Spacing = 16 };

        var recentFiles = _recentFilesService.GetAllRecentFiles(20);

        if (recentFiles.Count == 0)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "No recent files. Create a new document to get started.",
                FontSize = 14,
                Foreground = Application.Current.Resources["TextFillColorTertiaryBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 32)
            });
            return stack;
        }

        var listView = new ListView
        {
            ItemsSource = recentFiles,
            ItemTemplate = CreateRecentFileTemplate(),
            SelectionMode = ListViewSelectionMode.Single,
            BorderThickness = new Thickness(1),
            BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            CornerRadius = new CornerRadius(8),
            MaxHeight = 400
        };
        listView.DoubleTapped += (s, e) =>
        {
            if (listView.SelectedItem is RecentFile file)
            {
                OpenFile(file);
            }
        };

        stack.Children.Add(listView);
        return stack;
    }

    private DataTemplate CreateRecentFileTemplate()
    {
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Grid));
        factory.SetValue(Grid.ColumnDefinitionsProperty, new ColumnDefinitionCollection
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Auto }
        });

        // Icon
        var iconFactory = new FrameworkElementFactory(typeof(FontIcon));
        iconFactory.SetValue(FontIcon.GlyphProperty, new Binding { Path = new PropertyPath("AppId"), Converter = new AppIdToGlyphConverter() });
        iconFactory.SetValue(FontIcon.FontSizeProperty, 18);
        iconFactory.SetValue(FontIcon.ForegroundProperty, new Binding { Path = new PropertyPath("AppId"), Converter = new AppIdToBrushConverter() });
        iconFactory.SetValue(Grid.ColumnProperty, 0);
        iconFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(16, 12, 12, 12));
        iconFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        factory.AppendChild(iconFactory);

        // File info
        var infoStack = new FrameworkElementFactory(typeof(StackPanel));
        infoStack.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
        infoStack.SetValue(StackPanel.SpacingProperty, 2);
        infoStack.SetValue(Grid.ColumnProperty, 1);
        infoStack.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 12, 12, 12));
        infoStack.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        var nameFactory = new FrameworkElementFactory(typeof(TextBlock));
        nameFactory.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Name") });
        nameFactory.SetValue(TextBlock.FontSizeProperty, 14);
        nameFactory.SetValue(TextBlock.FontWeightProperty, Microsoft.UI.Text.FontWeights.Medium);
        nameFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextFillColorPrimaryBrush"]);
        infoStack.AppendChild(nameFactory);

        var pathFactory = new FrameworkElementFactory(typeof(TextBlock));
        pathFactory.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Path") });
        pathFactory.SetValue(TextBlock.FontSizeProperty, 12);
        pathFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextFillColorTertiaryBrush"]);
        pathFactory.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        infoStack.AppendChild(pathFactory);

        factory.AppendChild(infoStack);

        // Time
        var timeFactory = new FrameworkElementFactory(typeof(TextBlock));
        timeFactory.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("LastAccessed"), Converter = new DateTimeToRelativeConverter() });
        timeFactory.SetValue(TextBlock.FontSizeProperty, 11);
        timeFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextFillColorTertiaryBrush"]);
        timeFactory.SetValue(Grid.ColumnProperty, 2);
        timeFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 16, 0));
        timeFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        factory.AppendChild(timeFactory);

        // Pin button
        var pinFactory = new FrameworkElementFactory(typeof(Button));
        pinFactory.SetValue(Button.ContentProperty, new FontIcon { Glyph = "&#xE718;", FontSize = 14 });
        pinFactory.SetValue(Button.StyleProperty, Application.Current.Resources["TransparentButtonStyle"]);
        pinFactory.SetValue(Grid.ColumnProperty, 3);
        pinFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 16, 0));
        pinFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pinFactory.SetValue(Button.CommandProperty, new Binding { Path = new PropertyPath("PinCommand") });
        factory.AppendChild(pinFactory);

        template.VisualTree = factory;
        return template;
    }

    private void OpenFile(RecentFile file)
    {
        try
        {
            var exeName = file.AppId + ".exe";
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            var exePath = Path.Combine(currentDir, exeName);

            if (File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo(exePath, $"\"{file.Path}\"") { UseShellExecute = true });
            }
        }
        catch { }
    }

    private void LoadRecentFiles()
    {
        // Refresh would be called when recent files change
    }
}

/// <summary>
/// Converter for AppId to glyph.
/// </summary>
public sealed class AppIdToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString() switch
        {
            "Nova.Write" => "&#xE8A5;",
            "Nova.Grid" => "&#xE94A;",
            "Nova.Deck" => "&#xE7B4;",
            "Nova.Pdf" => "&#xE8A5;",
            _ => "&#xE7C3;"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for AppId to accent brush.
/// </summary>
public sealed class AppIdToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString() switch
        {
            "Nova.Write" => Application.Current.Resources["WriteAccentBrush"],
            "Nova.Grid" => Application.Current.Resources["GridAccentBrush"],
            "Nova.Deck" => Application.Current.Resources["DeckAccentBrush"],
            "Nova.Pdf" => Application.Current.Resources["PdfAccentBrush"],
            _ => Application.Current.Resources["HubAccentBrush"]
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for string to brush.
/// </summary>
public sealed class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hex && hex.StartsWith("#"))
        {
            return new SolidColorBrush(Color.FromArgb(
                255,
                byte.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber)));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for DateTime to relative time string.
/// </summary>
public sealed class DateTimeToRelativeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dt)
        {
            var diff = DateTime.Now - dt;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalDays < 1) return $"{(int)diff.TotalHours} hr ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} days ago";
            return dt.ToShortDateString();
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}