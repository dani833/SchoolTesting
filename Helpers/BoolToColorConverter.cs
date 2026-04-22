using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SchoolTesting.Helpers
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCorrect)
                return isCorrect ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.LightPink);
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}