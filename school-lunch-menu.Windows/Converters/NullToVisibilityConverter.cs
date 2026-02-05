using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SchoolLunchMenu.Converters;

/// <summary>
/// Converts a null/non-null value to <see cref="Visibility"/>.
/// Null = Visible (shows placeholder), Non-null = Collapsed (hides placeholder).
/// </summary>
public class NullToVisibleConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a null/non-null value to <see cref="Visibility"/>.
/// Null = Collapsed, Non-null = Visible.
/// </summary>
public class NullToCollapsedConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
