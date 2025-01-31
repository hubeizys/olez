using System;
using System.Globalization;
using System.Windows.Data;

namespace ollez.Converters
{
    public class FileSizeConverter : IValueConverter
    {
        private static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long size)
            {
                int order = 0;
                double len = size;

                while (len >= 1024 && order < SizeSuffixes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                return $"{len:0.##} {SizeSuffixes[order]}";
            }

            return "0 B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 