using System;
using System.Windows;
using System.Windows.Controls;

namespace FamilyEditorInterface
{
    /// <summary>
    /// Interaction logic for ProgressBar.xaml
    /// </summary>
    public partial class ProgressBarView : Window, IDisposable
    {
        private bool abortFlag;
        string _format;

        public ProgressBarView(Object dc)
        {
            this.DataContext = dc;

            InitializeComponent();
        }
        public void Dispose()
        {
            Close();
        }
        /// <summary>
        /// Set up progress bar form and immediately display it modelessly.
        /// </summary>
        /// <param name="caption">Form caption</param>
        /// <param name="format">Progress message string</param>
        /// <param name="max">Number of elements to process</param>
        public ProgressBarView(string caption, string format, int max)
        {
            InitializeComponent();

            _format = format;
            InitializeComponent();
            Title = caption;
            ProgressStatus.Text = (null == format) ? caption : string.Format(format, 0, "");
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = max;
            ProgressBar.Value = 0;
            Show();
            System.Windows.Forms.Application.DoEvents();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }
        

        public void Increment(string current)
        {
            ++ProgressBar.Value;
            if (null != _format)
            {
                ProgressStatus.Text = string.Format(_format, ProgressBar.Value, current);
            }
            System.Windows.Forms.Application.DoEvents();
        }

        public bool getAbortFlag()
        {
            return abortFlag;
        }        

        private void ProgressCancel_Click(object sender, RoutedEventArgs e)
        {
            var button = (sender as Button);
            button.Text = "Aborting...";
            abortFlag = true;
        }
    }
}
