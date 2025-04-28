using System.Globalization;

namespace SET09102_2024_5.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Active" => Colors.Green,
                    "Inactive" => Colors.Gray,
                    "Maintenance" => Colors.Orange,
                    "Error" => Colors.Red,
                    "Warning" => Colors.OrangeRed,
                    _ => Colors.Black
                };
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
