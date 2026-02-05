using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SchoolLunchMenu.Converters;

/// <summary>
/// Converts a boolean value to a <see cref="Visibility"/> value.
/// True = Collapsed, False = Visible (inverse of <see cref="BoolToVisibilityConverter"/>).
/// </summary>
public class BoolToCollapsedConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Visibility.Collapsed;
    }
}
