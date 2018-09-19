using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        // Only allow positive integers
        private static readonly Regex _regex = new Regex("[^0-9]+");

        public Settings()
        {
            try
            {
                InitializeComponent();
                txtAnswer.Text = Properties.Settings.Default.Precision.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            string result = txtAnswer.Text;
            if (result.Equals("") || !IsTextAllowed(result))
            {
                this.DialogResult = false;
            }
            else
            {
                Properties.Settings.Default.Precision = Int32.Parse(txtAnswer.Text);
                this.DialogResult = true;
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtAnswer.SelectAll();
            txtAnswer.Focus();
        }
        // Check if the input text matches the Regex
        private void txtAnswer_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }
        // Checks the input against the Regex (only positive integers)
        private static bool IsTextAllowed(string text)
        {
            int result = 0;
            if (Int32.TryParse(text, out result))
            {
                // Your conditions
                if (result >= 0 && result < 5)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
