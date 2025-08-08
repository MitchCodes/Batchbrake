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
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Drag and Drop Functionality
        private async void OnDrop(object sender, DragEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            if (viewModel != null)
            {
                viewModel.IsDraggingOver = false;
                
                // Don't allow dropping files while conversion is in progress
                if (viewModel.IsConverting)
                {
                    return;
                }
            }

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

            if (filesDropped.Count > 0 && viewModel != null)
            {
                var videoExtensions = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };
                foreach (var file in filesDropped)
                {
                    var extension = System.IO.Path.GetExtension(file).ToLowerInvariant();
                    if (videoExtensions.Contains(extension))
                    {
                        await viewModel.AddNewFile(file);
                    }
                }
            }
        }

        // Drag Enter - Visual feedback for drag and drop
        private void OnDragEnter(object sender, DragEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            
            // Don't allow drag operations while converting
            if (viewModel?.IsConverting == true)
            {
                e.DragEffects = DragDropEffects.None;
                return;
            }
            
            if (HasVideoFiles(e))
            {
                e.DragEffects = DragDropEffects.Copy;
                if (viewModel != null)
                {
                    viewModel.IsDraggingOver = true;
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        // Drag Over - Maintain visual feedback
        private void OnDragOver(object sender, DragEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            
            // Don't allow drag operations while converting
            if (viewModel?.IsConverting == true)
            {
                e.DragEffects = DragDropEffects.None;
                return;
            }
            
            if (HasVideoFiles(e))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        // Drag Leave - Remove visual feedback
        private void OnDragLeave(object sender, DragEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            if (viewModel != null)
            {
                viewModel.IsDraggingOver = false;
            }
        }

        // Helper method to check if drag contains video files
        private bool HasVideoFiles(DragEventArgs e)
        {
            var videoExtensions = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };

            if (e.Data.Contains(DataFormats.Files))
            {
                var files = e.Data.GetFiles();
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        var extension = System.IO.Path.GetExtension(file.Path.AbsolutePath).ToLowerInvariant();
                        if (videoExtensions.Contains(extension))
                        {
                            return true;
                        }
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
                        var extension = System.IO.Path.GetExtension(file).ToLowerInvariant();
                        if (videoExtensions.Contains(extension))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}