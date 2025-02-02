/*
 * 文件名：StringToUriConverter.cs
 * 创建者：yunsong
 * 创建时间：2024/02/03
 * 描述：string类型转Uri类型的转换器
 */

using System;
using System.Globalization;
using System.Windows.Data;

namespace ollez.Converters
{
    public class StringToUriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
            {
                return new Uri(stringValue);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Uri uri)
            {
                return uri.ToString();
            }
            return null;
        }
    }
}