using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SkiaSharp;

namespace QTSAvalonia.Utilities;

public static class Utility
{
    private static readonly FilePickerFileType QuadFileTypeFilter = new("quad")
    {
        Patterns = ["*.quad"]
    };

    public static IReadOnlyList<IStorageFile>? OpenImageFilePicker(IStorageProvider storageProvider)
    {
        var files = storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select images", SuggestedStartLocation = null, AllowMultiple = true, FileTypeFilter = [FilePickerFileTypes.ImageAll]
        }).Result;
        return files.Count == 0 ? null : files;
    }

    public static Bitmap ToBitmap(this SKBitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return new Bitmap(ms);
    }

    public static IReadOnlyList<IStorageFile>? OpenQuadFilePicker(IStorageProvider storageProvider)
    {
        var files = storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select quad", SuggestedStartLocation = null, AllowMultiple = false, FileTypeFilter = [QuadFileTypeFilter]
        }).Result;
        return files.Count == 0 ? null : files;
    }

    public static string DecodePath(this Uri uri)
    {
        return HttpUtility.UrlDecode(uri.AbsolutePath);
    }
}