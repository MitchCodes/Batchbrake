using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Batchbrake
{
    public partial class CustomPresetsHelpWindow : Window
    {
        public CustomPresetsHelpWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}