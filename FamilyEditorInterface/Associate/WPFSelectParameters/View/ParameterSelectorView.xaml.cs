using FamilyEditorInterface.Associate.WPFSelectParameters.ViewModel;
using System.Windows;
using System.Windows.Input;

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
        // ESC Button pressed triggers Window close        
        private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }
    }
}
