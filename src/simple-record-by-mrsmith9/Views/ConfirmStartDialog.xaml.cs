using System.Windows;

namespace SimpleRecord.Views
{
    /// <summary>
    /// A permission / confirmation step shown every time before a recording
    /// starts, so it's always clear what is about to happen. Pressing Enter
    /// confirms, Escape cancels - both work without touching the mouse.
    /// </summary>
    public partial class ConfirmStartDialog : Window
    {
        public ConfirmStartDialog(string sourceDescription)
        {
            InitializeComponent();
            SourceDescriptionRun.Text = $"This will record: {sourceDescription}.";
            Loaded += (_, _) => ConfirmButton.Focus();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
