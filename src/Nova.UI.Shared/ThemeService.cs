using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;

namespace Nova.UI.Shared;

/// <summary>
/// Theme service for managing application theme.
/// </summary>
public enum ThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2,
    HighContrast = 3
}

/// <summary>
/// Service for managing application theming including Mica/Acrylic backdrops.
/// </summary>
public sealed class ThemeService
{
    private readonly ISettingsService _settingsService;
    private ThemeMode _currentMode = ThemeMode.System;
    private MicaController? _micaController;
    private DesktopAcrylicController? _acrylicController;
    private Window? _window;

    public ThemeMode CurrentMode => _currentMode;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public ThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _currentMode = _settingsService.Current.Theme switch
        {
            AppTheme.System => ThemeMode.System,
            AppTheme.Light => ThemeMode.Light,
            AppTheme.Dark => ThemeMode.Dark,
            AppTheme.HighContrast => ThemeMode.HighContrast,
            _ => ThemeMode.System
        };

        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        if (e.SettingName == "Theme" && e.NewValue is AppTheme theme)
        {
            SetThemeMode(theme switch
            {
                AppTheme.System => ThemeMode.System,
                AppTheme.Light => ThemeMode.Light,
                AppTheme.Dark => ThemeMode.Dark,
                AppTheme.HighContrast => ThemeMode.HighContrast,
                _ => ThemeMode.System
            });
        }
    }

    /// <summary>
    /// Initialize the theme for a window.
    /// </summary>
    public void InitializeWindow(Window window)
    {
        _window = window;
        ApplyTheme();
        SetupBackdrop();
    }

    /// <summary>
    /// Set the theme mode.
    /// </summary>
    public void SetThemeMode(ThemeMode mode)
    {
        if (_currentMode == mode) return;

        var oldMode = _currentMode;
        _currentMode = mode;

        // Update settings
        _settingsService.Current.Theme = mode switch
        {
            ThemeMode.System => AppTheme.System,
            ThemeMode.Light => AppTheme.Light,
            ThemeMode.Dark => AppTheme.Dark,
            ThemeMode.HighContrast => AppTheme.HighContrast,
            _ => AppTheme.System
        };
        _ = _settingsService.SaveAsync();

        ApplyTheme();
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldMode, mode));
    }

    private void ApplyTheme()
    {
        if (_window?.Content is FrameworkElement root)
        {
            var actualTheme = ResolveActualTheme();
            root.RequestedTheme = actualTheme;
        }
    }

    private ElementTheme ResolveActualTheme()
    {
        return _currentMode switch
        {
            ThemeMode.Light => ElementTheme.Light,
            ThemeMode.Dark => ElementTheme.Dark,
            ThemeMode.HighContrast => ElementTheme.Default, // Uses system high contrast
            ThemeMode.System => GetSystemTheme(),
            _ => ElementTheme.Default
        };
    }

    private ElementTheme GetSystemTheme()
    {
        // Check system theme via UISettings
        var uiSettings = new Windows.UI.ViewManagement.UISettings();
        var color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
        return color.Lightness > 0.5 ? ElementTheme.Light : ElementTheme.Dark;
    }

    private void SetupBackdrop()
    {
        if (_window == null) return;

        TrySetMicaBackdrop();
    }

    private bool TrySetMicaBackdrop()
    {
        if (!MicaController.IsSupported())
            return TrySetAcrylicBackdrop();

        _micaController = new MicaController
        {
            Kind = MicaKind.BaseAlt
        };

        var hwnd = WindowNative.GetWindowHandle(_window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        _micaController.AddSystemBackdropTarget(appWindow);
        _micaController.SetSystemBackdropConfiguration(new SystemBackdropConfiguration
        {
            Theme = ResolveActualTheme() == ElementTheme.Dark ? SystemBackdropTheme.Dark : SystemBackdropTheme.Light
        });

        return true;
    }

    private bool TrySetAcrylicBackdrop()
    {
        if (!DesktopAcrylicController.IsSupported())
            return false;

        _acrylicController = new DesktopAcrylicController
        {
            TintColor = Colors.Transparent,
            TintOpacity = 0.8,
            FallbackColor = Colors.White
        };

        var hwnd = WindowNative.GetWindowHandle(_window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        _acrylicController.AddSystemBackdropTarget(appWindow);

        return true;
    }

    internal void UpdateBackdropTheme()
    {
        if (_micaController != null)
        {
            _micaController.SetSystemBackdropConfiguration(new SystemBackdropConfiguration
            {
                Theme = ResolveActualTheme() == ElementTheme.Dark ? SystemBackdropTheme.Dark : SystemBackdropTheme.Light
            });
        }
    }
}

/// <summary>
/// Event arguments for theme changes.
/// </summary>
public sealed class ThemeChangedEventArgs : EventArgs
{
    public ThemeMode OldMode { get; }
    public ThemeMode NewMode { get; }

    public ThemeChangedEventArgs(ThemeMode oldMode, ThemeMode newMode)
    {
        OldMode = oldMode;
        NewMode = newMode;
    }
}

/// <summary>
/// Extension methods for theme resources.
/// </summary>
public static class ThemeResources
{
    /// <summary>
    /// Get a theme-aware brush.
    /// </summary>
    public static SolidColorBrush GetBrush(string resourceKey)
    {
        if (Application.Current.Resources.TryGetValue(resourceKey, out var value) && value is SolidColorBrush brush)
        {
            return brush;
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    /// <summary>
    /// Get a theme-aware color.
    /// </summary>
    public static Color GetColor(string resourceKey)
    {
        if (Application.Current.Resources.TryGetValue(resourceKey, out var value) && value is Color color)
        {
            return color;
        }
        return Colors.Transparent;
    }
}

/// <summary>
/// Accent colors for each app.
/// </summary>
public static class AppAccents
{
    public static readonly Color Write = Color.FromArgb(255, 0, 120, 255);      // Electric Blue
    public static readonly Color Grid = Color.FromArgb(255, 0, 180, 90);        // Emerald
    public static readonly Color Deck = Color.FromArgb(255, 255, 160, 0);       // Amber
    public static readonly Color Pdf = Color.FromArgb(255, 220, 20, 60);        // Crimson
    public static readonly Color Hub = Color.FromArgb(255, 140, 80, 255);       // Violet
}