using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ollez.Converters
{
    public class UserMessageBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUser)
            {
                var primaryColor = System.Windows.Application.Current.Resources["MaterialDesignPrimary"] as Color?;
                var paperColor = System.Windows.Application.Current.Resources["MaterialDesignPaper"] as Color?;

                if (primaryColor.HasValue && paperColor.HasValue)
                {
                    return isUser
                        ? new SolidColorBrush(primaryColor.Value)
                        : new SolidColorBrush(paperColor.Value);
                }
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}