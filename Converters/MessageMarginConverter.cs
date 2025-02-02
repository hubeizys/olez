using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ollez.Converters
{
    public class MessageMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? new Thickness(100, 4, 4, 4) : new Thickness(4, 4, 100, 4);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
