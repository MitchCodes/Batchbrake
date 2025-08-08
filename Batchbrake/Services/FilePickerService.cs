using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batchbrake.Services
{
    public interface IFilePickerService
    {
        Task<IReadOnlyList<IStorageFile>?> OpenFilePickerAsync(FilePickerOpenOptions? filePickerOptions = null);
    }

    public class FilePickerService : IFilePickerService
    {
        private readonly TopLevel _topLevel;

        public FilePickerService(TopLevel topLevel)
        {
            _topLevel = topLevel;
        }

        public async Task<IReadOnlyList<IStorageFile>?> OpenFilePickerAsync(FilePickerOpenOptions? filePickerOptions = null)
        {
            if (filePickerOptions == null)
            {
                filePickerOptions = new FilePickerOpenOptions();
            }

            var files = await _topLevel.StorageProvider.OpenFilePickerAsync(filePickerOptions);

            return files;
        }
    }
}
