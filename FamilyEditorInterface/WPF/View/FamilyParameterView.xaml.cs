using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FamilyEditorInterface.WPF
{
    /// <summary>
    /// Int to Color Converter
    /// </summary>
    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value * 2;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (double)value / 2;
        }
    }
    /// <summary>
    /// Int to Color Converter
    /// </summary>
    public class RevitToUnitConverter : IValueConverter
    {       
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Round(Utils.convertValueTO((double)value),0);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Utils.convertValueFROM(Double.Parse((string)value.ToString()));
        }
    }
    /// <summary>
    /// Interaction logic for FamilyParameterView.xaml
    /// </summary>
    public partial class FamilyParameterView : Window
    {
        public FamilyParameterView(FamilyParameterViewModel vm)
        {
            this.DataContext = vm;

            InitializeComponent();
        }
    }
}
