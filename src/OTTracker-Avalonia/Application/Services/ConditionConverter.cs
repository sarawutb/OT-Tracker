using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace OTTracker_Avalonia.AppServices.Converters;

public sealed class ConditionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string paramStr)
        {
            var parts = paramStr.Split(';');
            if (parts.Length >= 2)
            {
                var valStr = boolValue ? parts[0] : parts[1];

                // 1. Check if targetType is IBrush
                if (targetType == typeof(IBrush))
                {
                    return Brush.Parse(valStr);
                }

                // 2. Check if targetType is Thickness
                if (targetType == typeof(Thickness))
                {
                    return Thickness.Parse(valStr);
                }

                // 3. Check if targetType is FontWeight
                if (targetType == typeof(FontWeight))
                {
                    return valStr.Equals("Bold", StringComparison.OrdinalIgnoreCase) 
                        ? FontWeight.Bold 
                        : FontWeight.Normal;
                }

                // 4. Fallback conversion for primitive types
                try
                {
                    return System.Convert.ChangeType(valStr, targetType, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return valStr;
                }
            }
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
