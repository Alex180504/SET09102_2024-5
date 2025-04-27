using System;
using System.Globalization;
using System.ComponentModel;

namespace SET09102_2024_5.Converters
{
    /// <summary>
    /// Converts a null value to a boolean value. Returns true if the value is not null, false if null.
    /// Can be inverted with a parameter.
    /// </summary>
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            bool invert = parameter != null && parameter.ToString().ToLower() == "invert";
            
            return invert ? isNull : !isNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter is typically used for one-way bindings
            // Default implementation returns null
            return null;
        }
    }
}