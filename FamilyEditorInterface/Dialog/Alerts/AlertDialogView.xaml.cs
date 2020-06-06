using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Dialog.Alerts
{
    /// <summary>
    /// Interaction logic for AlertDialogView.xaml
    /// </summary>
    public partial class AlertDialogView : UserControl
    {
        public AlertDialogView()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// Converts element == null to visibility
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
