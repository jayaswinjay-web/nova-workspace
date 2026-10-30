using Microsoft.UI.Xaml.Data;

namespace Nova.UI.Shared;

/// <summary>
/// Converts zoom factor (0.0-10.0) to percentage string.
/// </summary>
public sealed class ZoomToPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double zoom)
        {
            return $"{(int)Math.Round(zoom * 100)}%";
        }
        return "100%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string text)
        {
            var clean = text.Trim().TrimEnd('%');
            if (double.TryParse(clean, out var percent))
            {
                return Math.Clamp(percent / 100.0, 0.05, 10.0);
            }
        }
        return 1.0;
    }
}