using Avalonia.Controls;
using Batchbrake.ViewModels;

namespace Batchbrake
{
    public partial class SegmentEditorWindow : Window
    {
        public SegmentEditorWindow()
        {
            InitializeComponent();
        }

        public SegmentEditorWindow(VideoModelViewModel videoModel) : this()
        {
            DataContext = new SegmentEditorViewModel(videoModel);
            
            // Handle dialog result
            if (DataContext is SegmentEditorViewModel viewModel)
            {
                viewModel.DialogResult += (sender, result) =>
                {
                    Close(result);
                };
            }
        }
    }
}