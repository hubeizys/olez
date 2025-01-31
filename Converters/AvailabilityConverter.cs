/*
 * 文件名：AvailabilityConverter.cs
 * 创建者：yunsong
 * 创建时间：2024/03/21
 * 描述：布尔值到可用性文本的转换器
 */

using System;
using System.Globalization;
using System.Windows.Data;

namespace ollez.Converters
{
    /// <summary>
    /// 将布尔值转换为"可用"/"不可用"文本的转换器
    /// </summary>
    public class AvailabilityConverter : IValueConverter
    {
        /// <summary>
        /// 将布尔值转换为可用性状态文本
        /// </summary>
        /// <param name="value">布尔值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>转换后的文本</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "可用" : "不可用";
        }

        /// <summary>
        /// 将可用性状态文本转换回布尔值（未实现）
        /// </summary>
        /// <exception cref="NotImplementedException">该方法未实现</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 