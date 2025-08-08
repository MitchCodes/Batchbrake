using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Batchbrake.ViewModels;

namespace Batchbrake
{
    public partial class PreferencesWindow : Window
    {
        public PreferencesWindow()
        {
            InitializeComponent();
        }

        public PreferencesWindow(PreferencesViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}