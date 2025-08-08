using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using System.Collections.Generic;
using System.Linq;
using Batchbrake.ViewModels;
using Avalonia.Platform.Storage;
using Batchbrake.Services;
using System;

namespace Batchbrake
{
    //https://github.com/AvaloniaUI/Avalonia.Samples/tree/main/src/Avalonia.Samples/CompleteApps/SimpleToDoList todo
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var topLevel = TopLevel.GetTopLevel(this);

            IFilePickerService filePickerService = new FilePickerService(topLevel);

            DataContext = new MainWindowViewModel(filePickerService);

            DragDrop.SetAllowDrop(this, true);

            AddHandler(DragDrop.DropEvent, OnDrop);
            AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Drag and Drop Functionality
        private async void OnDrop(object sender, DragEventArgs e)
        {
            List<string> filesDropped = new List<string>();

            if (e.Data.Contains(DataFormats.Text))
            {
                string file = e.Data.GetText();

                if (StorageProvider.TryGetFileFromPathAsync(file) != null)
                {
                    filesDropped.Add(file);
                }
            }
            else if (e.Data.Contains(DataFormats.Files))
            {
                var files = e.Data.GetFiles();

                if (files != null)
                {
                    foreach (var file in files)
                    {
                        filesDropped.Add(Uri.UnescapeDataString(file.Path.AbsolutePath));
                    }
                }
            }
            else if (e.Data.Contains(DataFormats.FileNames))
            {
                var files = e.Data.GetFileNames();
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        filesDropped.Add(Uri.UnescapeDataString(file));
                    }
                }
            }

            if (filesDropped.Count > 0)
            {
                foreach (var file in filesDropped)
                {
                    await ((MainWindowViewModel)DataContext).AddNewFile(file);
                }
            }
        }

        // Drag Enter - Visual feedback for drag and drop
        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.DragEffects = DragDropEffects.Copy;
        }
    }
}