using FamilyEditorInterface.Associate.WPFSelectParameters.ViewModel;
using System;
using System.Collections.Generic;
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

namespace FamilyEditorInterface.Associate.WPFSelectParameters.View
{
    /// <summary>
    /// Interaction logic for ParameterSelectorView.xaml
    /// </summary>
    public partial class ParameterSelectorView : Window
    {
        public ParameterSelectorView(ParameterSelectorViewModel vm)
        {
            this.DataContext = vm;

            InitializeComponent();
        }
    }
}
