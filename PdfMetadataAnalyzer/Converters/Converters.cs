using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PdfMetadataAnalyzer.Models;

namespace PdfMetadataAnalyzer.Converters;

[ValueConversion(typeof(ConfidenceLevel), typeof(Brush))]
public class ConfidenceColorConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        (ConfidenceLevel)v switch
        {
            ConfidenceLevel.Consensus => new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50)),
            ConfidenceLevel.High => new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF)),
            ConfidenceLevel.Medium => new SolidColorBrush(Color.FromRgb(0xD2, 0x99, 0x22)),
            ConfidenceLevel.Low => new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49)),
            _ => new SolidColorBrush(Color.FromRgb(0x48, 0x4F, 0x58)),
        };
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => ConfidenceLevel.None;
}

[ValueConversion(typeof(double), typeof(Brush))]
public class ScoreColorConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
    {
        double s = (double)v;
        return s switch
        {
            >= 90 => new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50)),
            >= 75 => new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF)),
            >= 50 => new SolidColorBrush(Color.FromRgb(0xD2, 0x99, 0x22)),
            _ => new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49)),
        };
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => 0.0;
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => (bool)v ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => (Visibility)v == Visibility.Visible;
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => (bool)v ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => (Visibility)v == Visibility.Collapsed;
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
    {
        bool isNull = v == null || (v is string s && string.IsNullOrWhiteSpace(s));
        bool inverse = p?.ToString()?.ToLower() == "inverse";
        if (inverse) return isNull ? Visibility.Visible : Visibility.Collapsed;
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => null!;
}

[ValueConversion(typeof(bool), typeof(string))]
public class BoolToYesNoConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => (bool)v ? "Yes" : "No";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => (string)v == "Yes";
}

[ValueConversion(typeof(bool), typeof(string))]
public class SuccessToIconConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => (bool)v ? "✓" : "✗";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => false;
}

[ValueConversion(typeof(bool), typeof(Brush))]
public class SuccessToColorConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => (bool)v
            ? new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50))
            : new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49));
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => false;
}

public class PdfTypeDisplayConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is PdfType type ? PdfTypeInfo.GetDisplayName(type) : v?.ToString() ?? "";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => null!;
}