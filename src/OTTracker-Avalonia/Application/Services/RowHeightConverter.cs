using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace OTTracker_Avalonia.AppServices.Converters;

public sealed class RowHeightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double hours = 0;
        if (value is decimal decVal)
        {
            hours = (double)decVal;
        }
        else if (value is double dblVal)
        {
            hours = dblVal;
        }
        else if (value is float fltVal)
        {
            hours = fltVal;
        }
        else if (value is int intVal)
        {
            hours = intVal;
        }

        string type = parameter as string ?? "Bar";
        if (type.Equals("Before", StringComparison.OrdinalIgnoreCase))
        {
            double val = hours <= 0 ? 1.0 : Math.Max(0.0, 12.0 - Math.Min(12.0, hours));
            return new GridLength(val, GridUnitType.Star);
        }
        else // "Bar"
        {
            double val = hours <= 0 ? 0.0 : Math.Min(12.0, hours);
            return new GridLength(val, GridUnitType.Star);
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
