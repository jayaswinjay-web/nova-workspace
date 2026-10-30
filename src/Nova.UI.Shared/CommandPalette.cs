using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Nova.UI.Shared;

/// <summary>
/// Command palette item.
/// </summary>
public partial class CommandPaletteItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _glyph = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private string _shortcut = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _isVisible = true;

    public IRelayCommand? Command { get; set; }
    public object? CommandParameter { get; set; }

    partial void OnTitleChanged(string value)
    {
        UpdateSearchText();
    }

    partial void OnDescriptionChanged(string value)
    {
        UpdateSearchText();
    }

    partial void OnCategoryChanged(string value)
    {
        UpdateSearchText();
    }

    private void UpdateSearchText()
    {
        SearchText = $"{Title} {Description} {Category} {Shortcut}".ToLowerInvariant();
    }
}

/// <summary>
/// Command palette service.
/// </summary>
public interface ICommandPaletteService
{
    ObservableCollection<CommandPaletteItem> Items { get; }
    void RegisterCommand(CommandPaletteItem item);
    void UnregisterCommand(string id);
    void ClearCommands();
}

/// <summary>
/// Implementation of command palette service.
/// </summary>
public sealed class CommandPaletteService : ICommandPaletteService
{
    public ObservableCollection<CommandPaletteItem> Items { get; } = new();

    public void RegisterCommand(CommandPaletteItem item)
    {
        if (!Items.Any(i => i.Id == item.Id))
        {
            Items.Add(item);
        }
    }

    public void UnregisterCommand(string id)
    {
        var item = Items.FirstOrDefault(i => i.Id == id);
        if (item != null)
        {
            Items.Remove(item);
        }
    }

    public void ClearCommands()
    {
        Items.Clear();
    }
}

/// <summary>
/// Command palette control.
/// </summary>
public sealed class CommandPalette : ContentDialog
{
    private readonly ICommandPaletteService _commandService;
    private readonly TextBox _searchBox;
    private readonly ListView _resultsList;
    private string _lastQuery = string.Empty;

    public CommandPalette(ICommandPaletteService commandService)
    {
        _commandService = commandService;
        _searchBox = new TextBox();
        _resultsList = new ListView();

        InitializeComponent();
        SetupEventHandlers();
    }

    private void InitializeComponent()
    {
        Title = "Command Palette";
        PrimaryButtonText = string.Empty;
        SecondaryButtonText = string.Empty;
        CloseButtonText = string.Empty;
        DefaultButton = ContentDialogButton.None;
        XamlRoot = App.MainWindow?.Content?.XamlRoot;

        // Style
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;

        // Content
        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            },
            Margin = new Thickness(24),
            MinWidth = 500,
            MaxWidth = 700
        };

        // Search box
        _searchBox.PlaceholderText = "Type a command... (Ctrl+Shift+P)";
        _searchBox.FontSize = 16;
        _searchBox.Padding = new Thickness(16, 12);
        _searchBox.BorderThickness = new Thickness(1);
        _searchBox.CornerRadius = new CornerRadius(8);
        Grid.SetRow(_searchBox, 0);
        grid.Children.Add(_searchBox);

        // Results list
        _resultsList.SelectionMode = ListViewSelectionMode.Single;
        _resultsList.ItemTemplate = CreateResultTemplate();
        _resultsList.ItemsSource = _commandService.Items;
        _resultsList.MaxHeight = 400;
        _resultsList.BorderThickness = new Thickness(1);
        _resultsList.CornerRadius = new CornerRadius(8);
        _resultsList.Margin = new Thickness(0, 8, 0, 0);
        Grid.SetRow(_resultsList, 1);
        grid.Children.Add(_resultsList);

        Content = grid;
    }

    private DataTemplate CreateResultTemplate()
    {
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Grid));
        factory.SetValue(Grid.ColumnDefinitionsProperty, new ColumnDefinitionCollection
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            new ColumnDefinition { Width = GridLength.Auto }
        });

        // Icon
        var iconFactory = new FrameworkElementFactory(typeof(FontIcon));
        iconFactory.SetValue(FontIcon.GlyphProperty, new Binding { Path = new PropertyPath("Glyph") });
        iconFactory.SetValue(FontIcon.FontSizeProperty, 18);
        iconFactory.SetValue(FontIcon.ForegroundProperty, Application.Current.Resources["TextFillColorSecondaryBrush"]);
        iconFactory.SetValue(Grid.ColumnProperty, 0);
        iconFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(16, 12, 12, 12));
        iconFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        factory.AppendChild(iconFactory);

        // Title and description
        var textStack = new FrameworkElementFactory(typeof(StackPanel));
        textStack.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
        textStack.SetValue(StackPanel.SpacingProperty, 2);
        textStack.SetValue(Grid.ColumnProperty, 1);
        textStack.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 12, 12, 12));
        textStack.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        var titleFactory = new FrameworkElementFactory(typeof(TextBlock));
        titleFactory.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Title") });
        titleFactory.SetValue(TextBlock.FontSizeProperty, 14);
        titleFactory.SetValue(TextBlock.FontWeightProperty, Microsoft.UI.Text.FontWeights.Medium);
        titleFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextFillColorPrimaryBrush"]);
        textStack.AppendChild(titleFactory);

        var descFactory = new FrameworkElementFactory(typeof(TextBlock));
        descFactory.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Description") });
        descFactory.SetValue(TextBlock.FontSizeProperty, 12);
        descFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextFillColorTertiaryBrush"]);
        descFactory.SetValue(TextBlock.VisibilityProperty, new Binding { Path = new PropertyPath("Description"), Converter = new StringToVisibilityConverter() });
        textStack.AppendChild(descFactory);

        factory.AppendChild(textStack);

        // Shortcut
        var shortcutFactory = new FrameworkElementFactory(typeof(TextBlock));
        shortcutFactory.SetValue(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Shortcut") });
        shortcutFactory.SetValue(TextBlock.FontSizeProperty, 12);
        shortcutFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextFillColorTertiaryBrush"]);
        shortcutFactory.SetValue(Grid.ColumnProperty, 2);
        shortcutFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 12, 16, 12));
        shortcutFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        factory.AppendChild(shortcutFactory);

        template.VisualTree = factory;
        return template;
    }

    private void SetupEventHandlers()
    {
        _searchBox.TextChanged += OnSearchTextChanged;
        _searchBox.KeyDown += OnSearchBoxKeyDown;
        _resultsList.KeyDown += OnResultsKeyDown;
        _resultsList.DoubleTapped += OnResultsDoubleTapped;
        Opened += OnOpened;
        Closed += OnClosed;
    }

    private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        _searchBox.Text = string.Empty;
        _searchBox.Focus(FocusState.Programmatic);
        FilterResults(string.Empty);
    }

    private void OnClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        _searchBox.Text = string.Empty;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        FilterResults(_searchBox.Text);
    }

    private void FilterResults(string query)
    {
        _lastQuery = query?.ToLowerInvariant() ?? string.Empty;

        foreach (var item in _commandService.Items)
        {
            item.IsVisible = string.IsNullOrEmpty(_lastQuery) ||
                             item.SearchText.Contains(_lastQuery);
        }

        // Select first visible item
        var firstVisible = _commandService.Items.FirstOrDefault(i => i.IsVisible && i.IsEnabled);
        if (firstVisible != null)
        {
            _resultsList.SelectedItem = firstVisible;
            _resultsList.ScrollIntoView(firstVisible);
        }
    }

    private void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Down)
        {
            MoveSelection(1);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Up)
        {
            MoveSelection(-1);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ExecuteSelected();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void OnResultsKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ExecuteSelected();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void OnResultsDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ExecuteSelected();
    }

    private void MoveSelection(int direction)
    {
        var visibleItems = _commandService.Items.Where(i => i.IsVisible && i.IsEnabled).ToList();
        if (visibleItems.Count == 0) return;

        var currentIndex = _resultsList.SelectedIndex;
        if (currentIndex < 0) currentIndex = 0;

        var newIndex = Math.Clamp(currentIndex + direction, 0, visibleItems.Count - 1);
        _resultsList.SelectedItem = visibleItems[newIndex];
        _resultsList.ScrollIntoView(visibleItems[newIndex]);
    }

    private void ExecuteSelected()
    {
        if (_resultsList.SelectedItem is CommandPaletteItem item && item.IsEnabled)
        {
            item.Command?.Execute(item.CommandParameter);
            Hide();
        }
    }

    /// <summary>
    /// Show the command palette.
    /// </summary>
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
/// Global keyboard shortcut handler for command palette.
/// </summary>
public sealed class CommandPaletteShortcutHandler
{
    private readonly CommandPalette _palette;
    private readonly Window _window;

    public CommandPaletteShortcutHandler(CommandPalette palette, Window window)
    {
        _palette = palette;
        _window = window;
        _window.Content.KeyDown += OnWindowKeyDown;
    }

    private async void OnWindowKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Ctrl+Shift+P or Alt+Q
        var ctrl = Windows.UI.Core.CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var shift = Windows.UI.Core.CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var alt = Windows.UI.Core.CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if ((ctrl && shift && e.Key == Windows.System.VirtualKey.P) ||
            (alt && e.Key == Windows.System.VirtualKey.Q))
        {
            e.Handled = true;
            await _palette.ShowAsync();
        }
    }
}