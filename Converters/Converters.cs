using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PCBooster.Converters
{
    /// <summary>Maps a 0–100 percent value to Green / Amber / Red brush.</summary>
    public class PercentToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d >= 80) return new SolidColorBrush(Color.FromRgb(0xFF, 0x5F, 0x57)); // Red
                if (d >= 60) return new SolidColorBrush(Color.FromRgb(0xFE, 0xBC, 0x2E)); // Amber
                return new SolidColorBrush(Color.FromRgb(0x28, 0xC8, 0x40));               // Green
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Maps bool → Visibility (true = Visible, false = Collapsed).</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }

    /// <summary>Maps bool isDone → button label string ("Fix" vs "✓ Done").</summary>
    public class ActionDoneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? "✓ Done" : parameter?.ToString() ?? "Run";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
