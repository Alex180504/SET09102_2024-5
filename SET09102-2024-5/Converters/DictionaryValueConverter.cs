using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Maui.Controls;   // ← use MAUI’s binding interface

namespace SET09102_2024_5.Converters
{
    public class DictionaryValueConverter : IValueConverter
    {
        // value: the Dictionary<string,double>; parameter: the key (selected parameter)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Dictionary<string, double> dict
                && parameter is string key
                && dict.TryGetValue(key, out var val))
            {
                return val;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
