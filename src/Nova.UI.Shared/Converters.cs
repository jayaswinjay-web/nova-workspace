using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Nova.UI.Shared;

/// <summary>
/// Converts ControlSize to Orientation.
/// </summary>
public sealed class SizeToOrientationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is ControlSize size && size == ControlSize.Large ? Orientation.Vertical : Orientation.Horizontal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts ControlSize to FontSize.
/// </summary>
public sealed class SizeToFontSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is ControlSize size ? size switch
        {
            ControlSize.Small => 14,
            ControlSize.Normal => 16,
            ControlSize.Large => 24,
            _ => 16
        } : 16;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts ControlSize to label Visibility.
/// </summary>
public sealed class SizeToLabelVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is ControlSize size && size != ControlSize.Small ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts string to Visibility (collapsed if null/empty).
/// </summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to Thickness.
/// </summary>
public sealed class BoolToThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b && parameter is string param && int.TryParse(param, out var thickness))
        {
            return b ? new Thickness(thickness) : new Thickness(1);
        }
        return new Thickness(1);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to Visibility (inverted).
/// </summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}