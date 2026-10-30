using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Nova.UI.Shared;

/// <summary>
/// Backstage page types.
/// </summary>
public enum BackstagePageType
{
    Home,
    New,
    Open,
    SaveAs,
    Export,
    Print,
    Settings,
    About,
    Recent,
    Account
}

/// <summary>
/// Backstage item for navigation.
/// </summary>
public partial class BackstageItem : ObservableObject
{
    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private string _glyph = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private BackstagePageType _pageType;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _isSelected;

    public IRelayCommand? Command { get; set; }
}

/// <summary>
/// Template item for new document gallery.
/// </summary>
public partial class TemplateItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private ImageSource? _thumbnail;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private bool _isBuiltIn = true;

    public IRelayCommand? CreateCommand { get; set; }
}

/// <summary>
/// Backstage view model.
/// </summary>
public partial class BackstageViewModel : ObservableObject
{
    private readonly IRecentFilesService _recentFilesService;
    private readonly ISettingsService _settingsService;
    private readonly string _appId;

    [ObservableProperty]
    private ObservableCollection<BackstageItem> _navigationItems = new();

    [ObservableProperty]
    private ObservableCollection<TemplateItem> _templates = new();

    [ObservableProperty]
    private ObservableCollection<RecentFile> _recentFiles = new();

    [ObservableProperty]
    private ObservableCollection<RecentFile> _pinnedFiles = new();

    [ObservableProperty]
    private BackstagePageType _currentPage = BackstagePageType.Home;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    public BackstageViewModel(IRecentFilesService recentFilesService, ISettingsService settingsService, string appId)
    {
        _recentFilesService = recentFilesService;
        _settingsService = settingsService;
        _appId = appId;

        InitializeNavigation();
        LoadRecentFiles();
        _recentFilesService.RecentFilesChanged += OnRecentFilesChanged;
    }

    private void InitializeNavigation()
    {
        NavigationItems = new ObservableCollection<BackstageItem>
        {
            new BackstageItem
            {
                Label = "Home",
                Glyph = "&#xE80F;",
                PageType = BackstagePageType.Home,
                Description = "Create a new document or open an existing one"
            },
            new BackstageItem
            {
                Label = "New",
                Glyph = "&#xE710;",
                PageType = BackstagePageType.New,
                Description = "Create a new document from a template"
            },
            new BackstageItem
            {
                Label = "Open",
                Glyph = "&#xE8E5;",
                PageType = BackstagePageType.Open,
                Description = "Open an existing document"
            },
            new BackstageItem
            {
                Label = "Save As",
                Glyph = "&#xE74E;",
                PageType = BackstagePageType.SaveAs,
                Description = "Save a copy of the document"
            },
            new BackstageItem
            {
                Label = "Export",
                Glyph = "&#xE74F;",
                PageType = BackstagePageType.Export,
                Description = "Export to PDF or other formats"
            },
            new BackstageItem
            {
                Label = "Print",
                Glyph = "&#xE734;",
                PageType = BackstagePageType.Print,
                Description = "Print the document"
            },
            new BackstageItem
            {
                Label = "Recent",
                Glyph = "&#xE81C;",
                PageType = BackstagePageType.Recent,
                Description = "View recently opened documents"
            },
            new BackstageItem
            {
                Label = "Settings",
                Glyph = "&#xE713;",
                PageType = BackstagePageType.Settings,
                Description = "Configure application settings"
            },
            new BackstageItem
            {
                Label = "About",
                Glyph = "&#xE946;",
                PageType = BackstagePageType.About,
                Description = "View application information"
            }
        };

        // Select Home by default
        NavigationItems[0].IsSelected = true;
    }

    private void LoadRecentFiles()
    {
        RecentFiles = new ObservableCollection<RecentFile>(_recentFilesService.GetRecentFiles(_appId, 20));
        PinnedFiles = new ObservableCollection<RecentFile>(_recentFilesService.GetPinnedFiles(_appId));
    }

    private void OnRecentFilesChanged(object? sender, RecentFilesChangedEventArgs e)
    {
        if (e.AppId == _appId || e.AppId == "All")
        {
            LoadRecentFiles();
        }
    }

    [RelayCommand]
    private void Navigate(BackstagePageType pageType)
    {
        foreach (var item in NavigationItems)
        {
            item.IsSelected = item.PageType == pageType;
        }
        CurrentPage = pageType;

        if (pageType == BackstagePageType.New)
        {
            LoadTemplates();
        }
    }

    private void LoadTemplates()
    {
        // Built-in templates - in real app, load from disk/embedded resources
        Templates = new ObservableCollection<TemplateItem>
        {
            new TemplateItem
            {
                Id = "blank",
                Name = "Blank Document",
                Description = "Start with a clean slate",
                Category = "General",
                IsBuiltIn = true,
                CreateCommand = new RelayCommand(() => CreateFromTemplate("blank"))
            },
            new TemplateItem
            {
                Id = "letter",
                Name = "Business Letter",
                Description = "Professional letter format",
                Category = "Business",
                IsBuiltIn = true,
                CreateCommand = new RelayCommand(() => CreateFromTemplate("letter"))
            },
            new TemplateItem
            {
                Id = "resume",
                Name = "Resume",
                Description = "Clean, modern resume layout",
                Category = "Personal",
                IsBuiltIn = true,
                CreateCommand = new RelayCommand(() => CreateFromTemplate("resume"))
            },
            new TemplateItem
            {
                Id = "report",
                Name = "Report",
                Description = "Structured report with sections",
                Category = "Business",
                IsBuiltIn = true,
                CreateCommand = new RelayCommand(() => CreateFromTemplate("report"))
            },
            new TemplateItem
            {
                Id = "meeting-notes",
                Name = "Meeting Notes",
                Description = "Organized meeting minutes template",
                Category = "Business",
                IsBuiltIn = true,
                CreateCommand = new RelayCommand(() => CreateFromTemplate("meeting-notes"))
            },
            new TemplateItem
            {
                Id = "invoice",
                Name = "Invoice",
                Description = "Professional invoice template",
                Category = "Business",
                IsBuiltIn = true,
                CreateCommand = new RelayCommand(() => CreateFromTemplate("invoice"))
            }
        };
    }

    private void CreateFromTemplate(string templateId)
    {
        // This would be handled by the app-specific logic
        TemplateCreated?.Invoke(this, new TemplateCreatedEventArgs(templateId));
    }

    [RelayCommand]
    private async Task PinFile(RecentFile file)
    {
        await _recentFilesService.SetPinnedAsync(file.Path, true);
    }

    [RelayCommand]
    private async Task UnpinFile(RecentFile file)
    {
        await _recentFilesService.SetPinnedAsync(file.Path, false);
    }

    [RelayCommand]
    private async Task RemoveFile(RecentFile file)
    {
        await _recentFilesService.RemoveAsync(file.Path);
    }

    [RelayCommand]
    private async Task OpenFile(RecentFile file)
    {
        FileOpenRequested?.Invoke(this, new FileOpenRequestedEventArgs(file.Path));
    }

    partial void OnSearchQueryChanged(string value)
    {
        IsSearching = !string.IsNullOrWhiteSpace(value);
        // Filter recent files based on search query
        // Implementation would filter the RecentFiles collection
    }

    public event EventHandler<TemplateCreatedEventArgs>? TemplateCreated;
    public event EventHandler<FileOpenRequestedEventArgs>? FileOpenRequested;
    public event EventHandler<BackstagePageChangedEventArgs>? PageChanged;
}

/// <summary>
/// Event arguments for template creation.
/// </summary>
public sealed class TemplateCreatedEventArgs : EventArgs
{
    public string TemplateId { get; }

    public TemplateCreatedEventArgs(string templateId)
    {
        TemplateId = templateId;
    }
}

/// <summary>
/// Event arguments for file open request.
/// </summary>
public sealed class FileOpenRequestedEventArgs : EventArgs
{
    public string FilePath { get; }

    public FileOpenRequestedEventArgs(string filePath)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Event arguments for backstage page change.
/// </summary>
public sealed class BackstagePageChangedEventArgs : EventArgs
{
    public BackstagePageType PageType { get; }

    public BackstagePageChangedEventArgs(BackstagePageType pageType)
    {
        PageType = pageType;
    }
}

/// <summary>
/// Backstage (File menu) control.
/// </summary>
public sealed class Backstage : ContentDialog
{
    private readonly BackstageViewModel _viewModel;
    private readonly ListView _navigationList;
    private readonly Frame _contentFrame;

    public Backstage(BackstageViewModel viewModel)
    {
        _viewModel = viewModel;
        _navigationList = new ListView();
        _contentFrame = new Frame();

        InitializeComponent();
        SetupNavigation();
    }

    private void InitializeComponent()
    {
        Title = string.Empty;
        PrimaryButtonText = string.Empty;
        SecondaryButtonText = string.Empty;
        CloseButtonText = string.Empty;
        DefaultButton = ContentDialogButton.None;
        FullSizeDesired = true;
        XamlRoot = App.MainWindow?.Content?.XamlRoot;

        Style = Application.Current.Resources["FullScreenContentDialogStyle"] as Style;

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(280) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };

        // Navigation pane
        var navBorder = new Border
        {
            Background = new SolidColorColor(Color.FromArgb(255, 32, 32, 32)),
            BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(0, 0, 1, 0)
        };

        var navGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },  // App header
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },  // Navigation items
                new RowDefinition { Height = GridLength.Auto }   // Account/User
            }
        };

        // App header
        var headerPanel = new StackPanel
        {
            Margin = new Thickness(24, 24, 24, 16),
            Spacing = 8
        };
        headerPanel.Children.Add(new TextBlock
        {
            Text = "NOVA Office",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = Application.Current.Resources["TextFillColorPrimaryBrush"]
        });
        headerPanel.Children.Add(new TextBlock
        {
            Text = "Futuristic Fluent Design",
            FontSize = 13,
            Foreground = Application.Current.Resources["TextFillColorTertiaryBrush"]
        });
        Grid.SetRow(headerPanel, 0);
        navGrid.Children.Add(headerPanel);

        // Navigation list
        _navigationList.ItemsSource = _viewModel.NavigationItems;
        _navigationList.ItemTemplate = CreateNavigationItemTemplate();
        _navigationList.SelectionMode = ListViewSelectionMode.Single;
        _navigationList.Background = new SolidColorBrush(Colors.Transparent);
        _navigationList.BorderThickness = new Thickness(0);
        _navigationList.Padding = new Thickness(0, 8, 0, 8);
        Grid.SetRow(_navigationList, 1);
        navGrid.Children.Add(_navigationList);

        // Account area
        var accountPanel = new Border
        {
            Background = Application.Current.Resources["ControlFillColorDefaultBrush"],
            BorderBrush = Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 12),
            Margin = new Thickness(0, 8, 0, 0)
        };
        accountPanel.Child = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "Account",
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.Medium,
                    Foreground = Application.Current.Resources["TextFillColorPrimaryBrush"]
                },
                new TextBlock
                {
                    Text = "Sign in to sync settings",
                    FontSize = 12,
                    Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
                },
                new Button
                {
                    Content = "Sign In",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 4, 0, 0)
                }
            }
        };
        Grid.SetRow(accountPanel, 2);
        navGrid.Children.Add(accountPanel);

        navBorder.Child = navGrid;
        Grid.SetColumn(navBorder, 0);
        grid.Children.Add(navBorder);

        // Content area
        var contentBorder = new Border
        {
            Background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
            Padding = new Thickness(32)
        };
        contentBorder.Child = _contentFrame;
        Grid.SetColumn(contentBorder, 1);
        grid.Children.Add(contentBorder);

        Content = grid;
    }

    private DataTemplate CreateNavigationItemTemplate()
    {
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Grid));
        factory.SetValue(Grid.ColumnDefinitionsProperty, new ColumnDefinitionCollection
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
        });

        var iconFactory = new FrameworkElementFactory(typeof(FontIcon));
        iconFactory.SetValue(FontIcon.GlyphProperty, new Binding { Path = new PropertyPath("Glyph") });
        iconFactory.SetValue(FontIcon.FontSizeProperty, 18);
        iconFactory.SetValue(FontIcon.ForegroundProperty, new Binding { Path = new PropertyPath("IsSelected"), Converter = new SelectedToBrushConverter() });
        iconFactory.SetValue(Grid.ColumnProperty, 0);
        iconFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(24, 12, 16, 12));
        iconFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        factory.AppendChild(iconFactory);

        var textStack = new FrameworkElementFactory(typeof(StackPanel));
        textStack.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
        textStack.SetValue(StackPanel.SpacingProperty, 2);
        textStack.SetValue(Grid.ColumnProperty, 1);
        textStack.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 12, 24, 12));
        textStack.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        var labelFactory = new FrameworkElementFactory(typeof(TextBlock));
        labelFactory.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Label") });
        labelFactory.SetValue(TextBlock.FontSizeProperty, 14);
        labelFactory.SetValue(TextBlock.FontWeightProperty, Microsoft.UI.Text.FontWeights.Medium);
        labelFactory.SetValue(TextBlock.ForegroundProperty, new Binding { Path = new PropertyPath("IsSelected"), Converter = new SelectedToBrushConverter() });
        textStack.AppendChild(labelFactory);

        var descFactory = new FrameworkElementFactory(typeof(TextBlock));
        descFactory.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Description") });
        descFactory.SetValue(TextBlock.FontSizeProperty, 12);
        descFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextFillColorTertiaryBrush"]);
        textStack.AppendChild(descFactory);

        factory.AppendChild(textStack);

        template.VisualTree = factory;
        return template;
    }

    private void SetupNavigation()
    {
        _navigationList.SelectionChanged += OnNavigationSelectionChanged;
        _viewModel.Navigate(BackstagePageType.Home);
    }

    private void OnNavigationSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_navigationList.SelectedItem is BackstageItem item)
        {
            _viewModel.Navigate(item.PageType);
            LoadPageContent(item.PageType);
        }
    }

    private void LoadPageContent(BackstagePageType pageType)
    {
        _contentFrame.Content = pageType switch
        {
            BackstagePageType.Home => CreateHomePage(),
            BackstagePageType.New => CreateNewPage(),
            BackstagePageType.Open => CreateOpenPage(),
            BackstagePageType.Recent => CreateRecentPage(),
            BackstagePageType.Settings => CreateSettingsPage(),
            BackstagePageType.About => CreateAboutPage(),
            _ => CreatePlaceholderPage(pageType.ToString())
        };
    }

    private FrameworkElement CreateHomePage()
    {
        var stack = new StackPanel { Spacing = 24 };

        // New blank button
        var newBlankButton = new Button
        {
            Content = CreateButtonContent("&#xE710;", "New Blank Document", "Start with a blank document"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 64,
            FontSize = 16,
            Style = Application.Current.Resources["AccentButtonStyle"] as Style
        };
        newBlankButton.Click += (s, e) => _viewModel.CreateFromTemplateCommand.Execute("blank");
        stack.Children.Add(newBlankButton);

        // Quick actions
        var actionsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        actionsPanel.Children.Add(CreateActionButton("&#xE8E5;", "Open File", "Open an existing document"));
        actionsPanel.Children.Add(CreateActionButton("&#xE74E;", "Save As", "Save a copy"));
        actionsPanel.Children.Add(CreateActionButton("&#xE74F;", "Export PDF", "Export to PDF"));
        actionsPanel.Children.Add(CreateActionButton("&#xE734;", "Print", "Print document"));

        stack.Children.Add(new TextBlock { Text = "Quick Actions", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 8) });
        stack.Children.Add(actionsPanel);

        // Recent files
        stack.Children.Add(new TextBlock { Text = "Recent", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 24, 0, 8) });

        var recentList = new ListView
        {
            ItemsSource = _viewModel.RecentFiles,
            ItemTemplate = CreateRecentFileTemplate(),
            SelectionMode = ListViewSelectionMode.Single,
            MaxHeight = 300,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8)
        };
        recentList.DoubleTapped += (s, e) =>
        {
            if (recentList.SelectedItem is RecentFile file)
            {
                _viewModel.OpenFileCommand.Execute(file);
            }
        };
        stack.Children.Add(recentList);

        return new ScrollViewer
        {
            Content = stack,
            VerticalScrollMode = ScrollMode.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private FrameworkElement CreateNewPage()
    {
        var stack = new StackPanel { Spacing = 24 };

        stack.Children.Add(new TextBlock { Text = "New Document", FontSize = 28, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });

        // Search box
        var searchBox = new TextBox
        {
            PlaceholderText = "Search templates...",
            Width = 400,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 16)
        };
        searchBox.TextChanged += (s, e) => _viewModel.SearchQuery = searchBox.Text;
        stack.Children.Add(searchBox);

        // Category filter
        var categoryPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var categories = new[] { "All", "General", "Business", "Personal", "Education" };
        foreach (var cat in categories)
        {
            var btn = new RadioButton
            {
                Content = cat,
                Tag = cat,
                GroupName = "TemplateCategory",
                IsChecked = cat == "All",
                Style = Application.Current.Resources["RadioButtonChipStyle"] as Style
            };
            btn.Checked += (s, e) => FilterTemplatesByCategory((s as RadioButton)?.Tag as string);
            categoryPanel.Children.Add(btn);
        }
        stack.Children.Add(categoryPanel);

        // Template gallery
        var gallery = new GridView
        {
            ItemsSource = _viewModel.Templates,
            ItemTemplate = CreateTemplateItemTemplate(),
            SelectionMode = ListViewSelectionMode.Single,
            MaxHeight = 500,
            ItemsPanel = new ItemsPanelTemplate
            {
                VisualTree = new FrameworkElementFactory(typeof(ItemsWrapGrid))
                {
                    { ItemsWrapGrid.OrientationProperty, Orientation.Horizontal },
                    { ItemsWrapGrid.ItemWidthProperty, 220.0 },
                    { ItemsWrapGrid.ItemHeightProperty, 280.0 }
                }
            }
        };
        gallery.ItemClick += (s, e) =>
        {
            if (e.ClickedItem is TemplateItem template)
            {
                template.CreateCommand?.Execute(null);
            }
        };
        gallery.IsItemClickEnabled = true;

        stack.Children.Add(gallery);

        return new ScrollViewer
        {
            Content = stack,
            VerticalScrollMode = ScrollMode.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private FrameworkElement CreateOpenPage()
    {
        var stack = new StackPanel { Spacing = 16 };

        stack.Children.Add(new TextBlock { Text = "Open Document", FontSize = 28, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        buttonPanel.Children.Add(new Button
        {
            Content = "Browse...",
            Style = Application.Current.Resources["AccentButtonStyle"] as Style,
            Width = 140
        });
        buttonPanel.Children.Add(new Button
        {
            Content = "Recent Files",
            Width = 140
        });
        stack.Children.Add(buttonPanel);

        return new ScrollViewer { Content = stack };
    }

    private FrameworkElement CreateRecentPage()
    {
        var stack = new StackPanel { Spacing = 16 };

        stack.Children.Add(new TextBlock { Text = "Recent Documents", FontSize = 28, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });

        // Pinned files
        if (_viewModel.PinnedFiles.Count > 0)
        {
            stack.Children.Add(new TextBlock { Text = "Pinned", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 8) });
            var pinnedList = new ListView
            {
                ItemsSource = _viewModel.PinnedFiles,
                ItemTemplate = CreateRecentFileTemplate(),
                SelectionMode = ListViewSelectionMode.Single,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
            stack.Children.Add(pinnedList);
        }

        // Recent files
        stack.Children.Add(new TextBlock { Text = "Recent", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 16, 0, 8) });
        var recentList = new ListView
        {
            ItemsSource = _viewModel.RecentFiles,
            ItemTemplate = CreateRecentFileTemplate(),
            SelectionMode = ListViewSelectionMode.Single,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8)
        };
        stack.Children.Add(recentList);

        return new ScrollViewer { Content = stack };
    }

    private FrameworkElement CreateSettingsPage()
    {
        var stack = new StackPanel { Spacing = 24, MaxWidth = 600 };

        stack.Children.Add(new TextBlock { Text = "Settings", FontSize = 28, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });

        // Theme
        var themePanel = new StackPanel { Spacing = 8 };
        themePanel.Children.Add(new TextBlock { Text = "Theme", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Medium });

        var themeCombo = new ComboBox
        {
            ItemsSource = Enum.GetValues(typeof(ThemeMode)),
            SelectedItem = _settingsService.Current.Theme,
            Width = 200,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        themeCombo.SelectionChanged += (s, e) =>
        {
            if (themeCombo.SelectedItem is ThemeMode mode)
            {
                // Apply theme
            }
        };
        themePanel.Children.Add(themeCombo);
        stack.Children.Add(themePanel);

        // Autosave interval
        var autosavePanel = new StackPanel { Spacing = 8 };
        autosavePanel.Children.Add(new TextBlock { Text = "Autosave Interval (seconds)", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Medium });
        var autosaveBox = new NumberBox
        {
            Value = _settingsService.Current.AutosaveIntervalSeconds,
            Minimum = 10,
            Maximum = 3600,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
            Width = 150,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        autosaveBox.ValueChanged += (s, e) =>
        {
            _settingsService.Current.AutosaveIntervalSeconds = (int)e.NewValue;
            _ = _settingsService.SaveAsync();
        };
        autosavePanel.Children.Add(autosaveBox);
        stack.Children.Add(autosavePanel);

        // Default font
        var fontPanel = new StackPanel { Spacing = 8 };
        fontPanel.Children.Add(new TextBlock { Text = "Default Font", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Medium });
        var fontBox = new ComboBox
        {
            ItemsSource = new[] { "Segoe UI Variable", "Calibri", "Arial", "Times New Roman", "Consolas" },
            SelectedItem = _settingsService.Current.DefaultFontFamily,
            Width = 250,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        fontPanel.Children.Add(fontBox);
        stack.Children.Add(fontPanel);

        return new ScrollViewer { Content = stack };
    }

    private FrameworkElement CreateAboutPage()
    {
        var stack = new StackPanel { Spacing = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

        stack.Children.Add(new Image
        {
            Source = new BitmapImage(new Uri("ms-appx:///Assets/AppIcon.png")),
            Width = 128,
            Height = 128
        });

        stack.Children.Add(new TextBlock { Text = "NOVA Office", FontSize = 32, FontWeight = Microsoft.UI.Text.FontWeights.Bold, TextAlignment = TextAlignment.Center });
        stack.Children.Add(new TextBlock { Text = "Version 1.0.0", FontSize = 16, Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"], TextAlignment = TextAlignment.Center });
        stack.Children.Add(new TextBlock { Text = "A futuristic native Windows office suite", FontSize = 14, Foreground = Application.Current.Resources["TextFillColorTertiaryBrush"], TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 16, 0, 0) });
        stack.Children.Add(new TextBlock { Text = "Built with WinUI 3, .NET 10, and ❤️", FontSize = 13, Foreground = Application.Current.Resources["TextFillColorTertiaryBrush"], TextAlignment = TextAlignment.Center });
        stack.Children.Add(new HyperlinkButton
        {
            Content = "GitHub Repository",
            NavigateUri = new Uri("https://github.com/novaoffice"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 24, 0, 0)
        });

        return new ScrollViewer { Content = stack };
    }

    private FrameworkElement CreatePlaceholderPage(string title)
    {
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Spacing = 16 };
        stack.Children.Add(new TextBlock { Text = title, FontSize = 28, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        stack.Children.Add(new TextBlock { Text = "Coming soon...", FontSize = 16, Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] });
        return new ScrollViewer { Content = stack };
    }

    private FrameworkElement CreateButtonContent(string glyph, string title, string description)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };

        var icon = new FontIcon { Glyph = glyph, FontSize = 24, Margin = new Thickness(16, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);

        var textStack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        textStack.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Medium });
        textStack.Children.Add(new TextBlock { Text = description, FontSize = 12, Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] });
        Grid.SetColumn(textStack, 1);
        grid.Children.Add(textStack);

        return grid;
    }

    private Button CreateActionButton(string glyph, string label, string tooltip)
    {
        var btn = new Button
        {
            Style = Application.Current.Resources["SecondaryButtonStyle"] as Style,
            Padding = new Thickness(16, 8),
            ToolTip = tooltip
        };
        var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        stack.Children.Add(new FontIcon { Glyph = glyph, FontSize = 16 });
        stack.Children.Add(new TextBlock { Text = label, FontSize = 13 });
        btn.Content = stack;
        return btn;
    }

    private DataTemplate CreateRecentFileTemplate()
    {
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Grid));
        factory.SetValue(Grid.ColumnDefinitionsProperty, new ColumnDefinitionCollection
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            new ColumnDefinition { Width = GridLength.Auto }
        });

        // File icon
        var iconFactory = new FrameworkElementFactory(typeof(FontIcon));
        iconFactory.SetValue(FontIcon.GlyphProperty, "&#xE8A5;");
        iconFactory.SetValue(FontIcon.FontSizeProperty, 18);
        iconFactory.SetValue(FontIcon.ForegroundProperty, Application.Current.Resources["TextFillColorSecondaryBrush"]);
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

        // Pin button
        var pinFactory = new FrameworkElementFactory(typeof(Button));
        pinFactory.SetValue(Button.ContentProperty, new FontIcon { Glyph = "&#xE718;", FontSize = 14 });
        pinFactory.SetValue(Button.StyleProperty, Application.Current.Resources["TransparentButtonStyle"]);
        pinFactory.SetValue(Grid.ColumnProperty, 2);
        pinFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 16, 0));
        pinFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pinFactory.SetValue(Button.CommandProperty, new Binding { Path = new PropertyPath("DataContext.PinFileCommand"), RelativeSource = new RelativeSource { Mode = RelativeSourceMode.TemplatedParent } });
        pinFactory.SetValue(Button.CommandParameterProperty, new Binding());
        factory.AppendChild(pinFactory);

        template.VisualTree = factory;
        return template;
    }

    private DataTemplate CreateTemplateItemTemplate()
    {
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(Border.BackgroundProperty, Application.Current.Resources["ControlFillColorDefaultBrush"]);
        factory.SetValue(Border.BorderBrushProperty, Application.Current.Resources["ControlStrokeColorDefaultBrush"]);
        factory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        factory.SetValue(Border.PaddingProperty, new Thickness(16));
        factory.SetValue(Border.MarginProperty, new Thickness(4));
        factory.SetValue(FrameworkElement.WidthProperty, 200.0);

        var stack = new FrameworkElementFactory(typeof(StackPanel));
        stack.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
        stack.SetValue(StackPanel.SpacingProperty, 12);

        // Thumbnail
        var thumbFactory = new FrameworkElementFactory(typeof(Border));
        thumbFactory.SetValue(Border.BackgroundProperty, Application.Current.Resources["ControlFillColorSecondaryBrush"]);
        thumbFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        thumbFactory.SetValue(Border.HeightProperty, 120.0);
        thumbFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        thumbFactory.SetValue(Border.ChildProperty, new FrameworkElementFactory(typeof(FontIcon))
        {
            { FontIcon.GlyphProperty, "&#xE8A5;" },
            { FontIcon.FontSizeProperty, 48.0 },
            { FontIcon.ForegroundProperty, Application.Current.Resources["TextFillColorTertiaryBrush"] },
            { FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center },
            { FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center }
        });
        stack.AppendChild(thumbFactory);

        // Name
        var nameFactory = new FrameworkElementFactory(typeof(TextBlock));
        nameFactory.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Name") });
        nameFactory.SetValue(TextBlock.FontSizeProperty, 14);
        nameFactory.SetValue(TextBlock.FontWeightProperty, Microsoft.UI.Text.FontWeights.Medium);
        nameFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextFillColorPrimaryBrush"]);
        nameFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        nameFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        stack.AppendChild(nameFactory);

        // Description
        var descFactory = new FrameworkElementFactory(typeof(TextBlock));
        descFactory.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Description") });
        descFactory.SetValue(TextBlock.FontSizeProperty, 12);
        descFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextFillColorSecondaryBrush"]);
        descFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        descFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        descFactory.SetValue(TextBlock.MaxLinesProperty, 2);
        stack.AppendChild(descFactory);

        // Category badge
        var catFactory = new FrameworkElementFactory(typeof(TextBlock));
        catFactory.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Category") });
        catFactory.SetValue(TextBlock.FontSizeProperty, 11);
        catFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["SystemAccentColorBrush"]);
        catFactory.SetValue(TextBlock.FontWeightProperty, Microsoft.UI.Text.FontWeights.Medium);
        catFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        catFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 8, 0, 0));
        stack.AppendChild(catFactory);

        factory.SetValue(Border.ChildProperty, stack);
        template.VisualTree = factory;
        return template;
    }

    private void FilterTemplatesByCategory(string? category)
    {
        // Filter logic would go here
    }

    public new async Task ShowAsync()
    {
        if (XamlRoot == null && App.MainWindow?.Content != null)
        {
            XamlRoot = App.MainWindow.Content.XamlRoot;
        }
        await base.ShowAsync();
    }
}

/// <summary>
/// Converter for selected state to brush.
/// </summary>
public sealed class SelectedToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool b && b
            ? Application.Current.Resources["SystemAccentColorBrush"]
            : Application.Current.Resources["TextFillColorSecondaryBrush"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}