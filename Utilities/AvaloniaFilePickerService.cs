using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace QTSAvalonia.Utilities;

public class AvaloniaFilePickerService
{
    private readonly Func<TopLevel?> _getTopLevel; // 延迟获取，避免构造时为空

    public AvaloniaFilePickerService(Func<TopLevel?> getTopLevel) 
        => _getTopLevel = getTopLevel;

    public async Task<IReadOnlyList<IStorageFile>?> OpenImageFilesAsync()
    {
        var topLevel = _getTopLevel?.Invoke();
        if (topLevel?.StorageProvider == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title          = "Select Images",
            AllowMultiple  = true,
            FileTypeFilter = [FilePickerFileTypes.ImageAll]
        });
        return files;
    }    
    public async Task<IReadOnlyList<IStorageFile>?> OpenQuadFileAsync()
    {
        var topLevel = _getTopLevel?.Invoke();
        if (topLevel?.StorageProvider == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title          = "Select quad",
            FileTypeFilter = [new FilePickerFileType("*.quad")]
        });
        return files;
    }
}