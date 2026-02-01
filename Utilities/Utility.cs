using System;
using System.Collections.Generic;
using System.Web;
using Avalonia.Platform.Storage;

namespace QTSAvalonia.Utilities;

public static class Utility
{
    private static readonly FilePickerFileType QuadFileTypeFilter = new("quad") { Patterns = ["*.quad"] };

    public static IReadOnlyList<IStorageFile>? OpenImageFilePicker(IStorageProvider storageProvider)
    {
        var files = storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title                  = "Select images",
            SuggestedStartLocation = null,
            AllowMultiple          = true,
            FileTypeFilter         = [FilePickerFileTypes.ImageAll]
        }).Result;
        return files.Count == 0 ? null : files;
    }

    public static IReadOnlyList<IStorageFile>? OpenQuadFilePicker(IStorageProvider storageProvider)
    {
        var files = storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title                  = "Select quad",
            SuggestedStartLocation = null,
            AllowMultiple          = false,
            FileTypeFilter         = [QuadFileTypeFilter]
        }).Result;
        return files.Count == 0 ? null : files;
    }

    public static string DecodePath(this Uri uri)
    {
        return HttpUtility.UrlDecode(uri.AbsolutePath);
    }
}