using System;
using System.Globalization;
using System.Windows.Data;

namespace ollez.Converters
{
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && parameter is string percentStr)
            {
                if (double.TryParse(percentStr, out double percent))
                {
                    return width * percent;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
