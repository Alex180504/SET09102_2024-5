// Converters/EnumToBoolConverter.cs
using System.Globalization;

namespace SET09102_2024_5.Converters
{
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            // Compare the enum value with the parameter
            return value.ToString().Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null || !(value is bool boolValue) || !boolValue)
                return null;

            // Only convert when value is true
            if (boolValue)
            {
                // Parse the parameter string as the target enum type
                return Enum.Parse(targetType, parameter.ToString());
            }

            return null;
        }
    }
}
