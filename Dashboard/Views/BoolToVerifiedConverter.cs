using System;
using System.Globalization;
using System.Windows.Data;

namespace Dashboard.Views
{
    public sealed class BoolToVerifiedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is true ? "Verified" : "Unverified";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is "Verified";
    }
}