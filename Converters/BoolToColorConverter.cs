using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace ollez.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isTrue)
            {
                if (parameter is string resourceKey)
                {
                    // 如果提供了资源键，从资源字典中获取颜色
                    return Application.Current.Resources[resourceKey];
                }

                // 默认颜色：已安装使用主题色，未安装使用灰色
                return isTrue

                    ? Application.Current.Resources["PrimaryHueMidBrush"]

                    : Application.Current.Resources["MaterialDesignBodyLight"];
            }
            return Application.Current.Resources["MaterialDesignBodyLight"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}