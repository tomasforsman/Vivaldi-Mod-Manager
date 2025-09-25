using System.Globalization;
using System.Windows.Data;

namespace VivaldiModManager.UI.Converters;

/// <summary>
/// Converts boolean values to status text.
/// </summary>
public class BoolToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "Active" : "Inactive";
        }
        
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}