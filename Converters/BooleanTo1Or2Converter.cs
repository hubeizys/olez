using System;
using System.Globalization;
using System.Windows.Data;

namespace ollez.Converters
{
    public class BooleanTo1Or2Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUser)
            {
                // 如果是用户消息返回1（中间列），否则返回0（第一列）
                return isUser ? 1 : 0;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}