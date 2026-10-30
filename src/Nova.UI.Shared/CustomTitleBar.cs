using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics;
using WinRT.Interop;

namespace Nova.UI.Shared;

/// <summary>
/// Custom title bar with app icon, document name, search box, and save status.
/// </summary>
public sealed partial class CustomTitleBar : UserControl
{
    private Window? _window;
    private AppWindow? _appWindow;
    private RectInt32[]? _dragRects;
    private readonly ThemeService _themeService;

    public static readonly DependencyProperty AppIconProperty =
        DependencyProperty.Register(nameof(AppIcon), typeof(ImageSource), typeof(CustomTitleBar), new PropertyMetadata(null));

    public static readonly DependencyProperty DocumentTitleProperty =
        DependencyProperty.Register(nameof(DocumentTitle), typeof(string), typeof(CustomTitleBar), new PropertyMetadata("Untitled"));

    public static readonly DependencyProperty SaveStatusProperty =
        DependencyProperty.Register(nameof(SaveStatus), typeof(SaveStatus), typeof(CustomTitleBar), new PropertyMetadata(SaveStatus.Saved, OnSaveStatusChanged));

    public static readonly DependencyProperty ShowSearchBoxProperty =
        DependencyProperty.Register(nameof(ShowSearchBox), typeof(bool), typeof(CustomTitleBar), new PropertyMetadata(true));

    public static readonly DependencyProperty SearchPlaceholderProperty =
        DependencyProperty.Register(nameof(SearchPlaceholder), typeof(string), typeof(CustomTitleBar), new PropertyMetadata("Search (Ctrl+Shift+P)"));

    public static readonly DependencyProperty CanMinimizeProperty =
        DependencyProperty.Register(nameof(CanMinimize), typeof(bool), typeof(CustomTitleBar), new PropertyMetadata(true));

    public static readonly DependencyProperty CanMaximizeProperty =
        DependencyProperty.Register(nameof(CanMaximize), typeof(bool), typeof(CustomTitleBar), new PropertyMetadata(true));

    public static readonly DependencyProperty CanCloseProperty =
        DependencyProperty.Register(nameof(CanClose), typeof(bool), typeof(CustomTitleBar), new PropertyMetadata(true));

    public ImageSource? AppIcon
    {
        get => (ImageSource?)GetValue(AppIconProperty);
        set => SetValue(AppIconProperty, value);
    }

    public string DocumentTitle
    {
        get => (string)GetValue(DocumentTitleProperty);
        set => SetValue(DocumentTitleProperty, value);
    }

    public SaveStatus SaveStatus
    {
        get => (SaveStatus)GetValue(SaveStatusProperty);
        set => SetValue(SaveStatusProperty, value);
    }

    public bool ShowSearchBox
    {
        get => (bool)GetValue(ShowSearchBoxProperty);
        set => SetValue(ShowSearchBoxProperty, value);
    }

    public string SearchPlaceholder
    {
        get => (string)GetValue(SearchPlaceholderProperty);
        set => SetValue(SearchPlaceholderProperty, value);
    }

    public bool CanMinimize
    {
        get => (bool)GetValue(CanMinimizeProperty);
        set => SetValue(CanMinimizeProperty, value);
    }

    public bool CanMaximize
    {
        get => (bool)GetValue(CanMaximizeProperty);
        set => SetValue(CanMaximizeProperty, value);
    }

    public bool CanClose
    {
        get => (bool)GetValue(CanCloseProperty);
        set => SetValue(CanCloseProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? MinimizeClick;
    public event EventHandler<RoutedEventArgs>? MaximizeClick;
    public event EventHandler<RoutedEventArgs>? CloseClick;
    public event EventHandler<SearchInvokedEventArgs>? SearchInvoked;

    public CustomTitleBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void Initialize(Window window, ThemeService themeService)
    {
        _window = window;
        _themeService = themeService;

        var hwnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        // Extend content into title bar
        _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        _appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        // Set drag region
        UpdateDragRegions();

        // Handle window size changes
        window.SizeChanged += OnWindowSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateDragRegions();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_window != null)
        {
            _window.SizeChanged -= OnWindowSizeChanged;
        }
    }

    private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
    {
        UpdateDragRegions();
    }

    private void UpdateDragRegions()
    {
        if (_appWindow == null) return;

        var transform = TitleBarGrid.TransformToVisual(null);
        var bounds = TitleBarGrid.Bounds;
        var topLeft = transform.TransformPoint(new Point(0, 0));
        var bottomRight = transform.TransformPoint(new Point(bounds.Width, bounds.Height));

        // Exclude button areas from drag region
        var searchBoxRect = SearchBoxGrid.TransformToVisual(null).TransformBounds(new Rect(0, 0, SearchBoxGrid.ActualWidth, SearchBoxGrid.ActualHeight));
        var buttonRect = WindowControlsGrid.TransformToVisual(null).TransformBounds(new Rect(0, 0, WindowControlsGrid.ActualWidth, WindowControlsGrid.ActualHeight));

        var dragRects = new List<RectInt32>();

        // Left of search box
        if (searchBoxRect.X > topLeft.X)
        {
            dragRects.Add(new RectInt32(
                (int)topLeft.X, (int)topLeft.Y,
                (int)(searchBoxRect.X - topLeft.X), (int)TitleBarGrid.ActualHeight));
        }

        // Right of search box, left of buttons
        if (buttonRect.X > searchBoxRect.Right)
        {
            dragRects.Add(new RectInt32(
                (int)searchBoxRect.Right, (int)topLeft.Y,
                (int)(buttonRect.X - searchBoxRect.Right), (int)TitleBarGrid.ActualHeight));
        }

        _dragRects = dragRects.ToArray();
        _appWindow.TitleBar.SetDragRectangles(_dragRects);
    }

    private static void OnSaveStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CustomTitleBar bar)
        {
            bar.UpdateSaveStatusVisual((SaveStatus)e.NewValue);
        }
    }

    private void UpdateSaveStatusVisual(SaveStatus status)
    {
        var color = status switch
        {
            SaveStatus.Saved => Colors.Green,
            SaveStatus.Saving => Colors.Orange,
            SaveStatus.Unsaved => Colors.Red,
            _ => Colors.Gray
        };

        SaveStatusIndicator.Fill = new SolidColorBrush(color);
        SaveStatusText.Text = status switch
        {
            SaveStatus.Saved => "Saved",
            SaveStatus.Saving => "Saving…",
            SaveStatus.Unsaved => "Unsaved",
            _ => "Unknown"
        };
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        MinimizeClick?.Invoke(this, e);
        _appWindow?.Minimize();
    }

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
    {
        MaximizeClick?.Invoke(this, e);
        if (_appWindow != null)
        {
            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                if (presenter.State == OverlappedPresenterState.Maximized)
                    presenter.Restore();
                else
                    presenter.Maximize();
            }
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        CloseClick?.Invoke(this, e);
        _window?.Close();
    }

    private void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            var query = SearchBox.Text;
            SearchInvoked?.Invoke(this, new SearchInvokedEventArgs(query));
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            SearchBox.Text = string.Empty;
            SearchBoxGrid.Focus(FocusState.Programmatic);
            e.Handled = true;
        }
    }

    private void OnSearchBoxLostFocus(object sender, RoutedEventArgs e)
    {
        // Clear search on focus loss if empty
        if (string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            SearchBox.Text = string.Empty;
        }
    }

    public void FocusSearchBox()
    {
        SearchBox.Focus(FocusState.Programmatic);
        SearchBox.SelectAll();
    }
}

/// <summary>
/// Save status enumeration.
/// </summary>
public enum SaveStatus
{
    Saved,
    Saving,
    Unsaved
}

/// <summary>
/// Event arguments for search invocation.
/// </summary>
public sealed class SearchInvokedEventArgs : EventArgs
{
    public string Query { get; }

    public SearchInvokedEventArgs(string query)
    {
        Query = query;
    }
}