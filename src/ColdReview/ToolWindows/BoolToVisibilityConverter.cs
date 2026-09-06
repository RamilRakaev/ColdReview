using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ColdReview.ToolWindows
{
    internal sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is true;
            if (value is string text)
            {
                flag = !string.IsNullOrWhiteSpace(text);
            }

            if (parameter as string == "Invert")
            {
                flag = !flag;
            }

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
