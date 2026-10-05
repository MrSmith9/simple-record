using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using RecorderByKyleSmith.Models;
using RecorderByKyleSmith.Services;

namespace RecorderByKyleSmith.Views
{
    /// <summary>
    /// Lets the user pick one open window to record, from a live list
    /// built by RecordingService. Fully usable with just the keyboard
    /// (arrow keys + Enter), or with the mouse (click, or double-click to
    /// select immediately).
    /// </summary>
    public partial class WindowSelectDialog : Window
    {
        public WindowInfo? SelectedWindow { get; private set; }

        public WindowSelectDialog()
        {
            InitializeComponent();
            LoadWindows();

            Loaded += (_, _) =>
            {
                if (WindowListBox.Items.Count > 0)
                {
                    WindowListBox.SelectedIndex = 0;
                }
                WindowListBox.Focus();
            };
        }

        private void LoadWindows()
        {
            List<WindowInfo> windows = RecordingService.GetRecordableWindows();
            WindowListBox.ItemsSource = windows;
            EmptyListText.Visibility = windows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadWindows();
            if (WindowListBox.Items.Count > 0)
            {
                WindowListBox.SelectedIndex = 0;
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            TryConfirmSelection();
        }

        private void WindowListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TryConfirmSelection();
        }

        private void TryConfirmSelection()
        {
            if (WindowListBox.SelectedItem is WindowInfo info)
            {
                SelectedWindow = info;
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
