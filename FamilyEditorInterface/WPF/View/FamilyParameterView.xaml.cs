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
    public static class InputBindingsManager
    {

        public static readonly DependencyProperty UpdatePropertySourceWhenEnterPressedProperty = DependencyProperty.RegisterAttached(
                "UpdatePropertySourceWhenEnterPressed", typeof(DependencyProperty), typeof(InputBindingsManager), new PropertyMetadata(null, OnUpdatePropertySourceWhenEnterPressedPropertyChanged));

        static InputBindingsManager()
        {

        }

        public static void SetUpdatePropertySourceWhenEnterPressed(DependencyObject dp, DependencyProperty value)
        {
            dp.SetValue(UpdatePropertySourceWhenEnterPressedProperty, value);
        }

        public static DependencyProperty GetUpdatePropertySourceWhenEnterPressed(DependencyObject dp)
        {
            return (DependencyProperty)dp.GetValue(UpdatePropertySourceWhenEnterPressedProperty);
        }

        private static void OnUpdatePropertySourceWhenEnterPressedPropertyChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = dp as UIElement;

            if (element == null)
            {
                return;
            }

            if (e.OldValue != null)
            {
                element.PreviewKeyDown -= HandlePreviewKeyDown;
            }

            if (e.NewValue != null)
            {
                element.PreviewKeyDown += new KeyEventHandler(HandlePreviewKeyDown);
            }
        }

        static void HandlePreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DoUpdateSource(e.Source);
            }
        }

        static void DoUpdateSource(object source)
        {
            DependencyProperty property =
                GetUpdatePropertySourceWhenEnterPressed(source as DependencyObject);

            if (property == null)
            {
                return;
            }

            UIElement elt = source as UIElement;

            if (elt == null)
            {
                return;
            }

            BindingExpression binding = BindingOperations.GetBindingExpression(elt, property);

            if (binding != null)
            {
                binding.UpdateSource();
            }
        }
    }
}
