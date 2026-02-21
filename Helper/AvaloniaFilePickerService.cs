using Avalonia.Platform.Storage;

namespace QTSAvalonia.Helper;

public static class AvaloniaFilePickerService
{
    private static TopLevel? _topLevel;

    public static void Initialize(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    public static async Task<IReadOnlyList<IStorageFile>?> OpenImageFilesAsync()
    {
        if (_topLevel?.StorageProvider == null) return null;

        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Images",
            AllowMultiple = true,
            FileTypeFilter = [FilePickerFileTypes.ImageAll]
        });
        return files;
    }

    public static async Task<IReadOnlyList<IStorageFile>?> OpenQuadFileAsync()
    {
        if (_topLevel?.StorageProvider == null) return null;

        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select quad",
            FileTypeFilter = [new FilePickerFileType("Quad files") { Patterns = ["*.quad"] }]
        });
        return files;
    }

    public static async Task<IReadOnlyList<IStorageFolder>?> OpenFileSavePathAsync()
    {
        if (_topLevel?.StorageProvider == null) return null;

        var folder = await _topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Save Path"
        });
        return folder;
    }
}