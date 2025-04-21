using System.Globalization;

namespace SET09102_2024_5.Converters
{
    public class BoolToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isVisible && isVisible)
            {
                // When visible, use the parameter as height
                if (parameter is string heightStr && double.TryParse(heightStr, out double height))
                {
                    return height;
                }
                return 200; // Default height
            }

            // When not visible, return a zero height
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
