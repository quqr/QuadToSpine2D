using System.Web;
using Avalonia.Platform.Storage;

namespace QuadToSpine2D.Tools;

public static class Utility
{
    public static IReadOnlyList<IStorageFile>? OpenImageFilePicker(IStorageProvider storageProvider)
    {
        var files = storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Images",
            SuggestedStartLocation = null,
            AllowMultiple = true,
            FileTypeFilter = [FilePickerFileTypes.ImageAll]
        }).Result;
        return files.Count == 0 ? null : files;
    }

    public static IReadOnlyList<IStorageFile>? OpenQuadFilePicker(IStorageProvider storageProvider)
    {
        var files = storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "QuadFile",
            SuggestedStartLocation = null,
            AllowMultiple = false,
            FileTypeFilter = null
        }).Result;
        return files.Count == 0 ? null : files;
    }

    public static string ConvertUriToPath(Uri uri)
    {
        return HttpUtility.UrlDecode(uri.AbsolutePath);
    }

    public static List<List<string?>> ConvertImagePath(List<List<string?>?> imagePath)
    {
        List<List<string?>> result = [];
        imagePath.RemoveAll(x => x is null);
        var maxCount = imagePath.MaxBy(x => x.Count).Count;
        for (var i = 0; i < imagePath.Count; i++)
        for (var j = 0; j < maxCount; j++)
        {
            if (result.Count < maxCount) result.Add([]);
            if (j > imagePath[i].Count)
            {
                result[j].Add(null);
                continue;
            }

            result[j].Add(imagePath[i][j]);
        }

        return result;
    }
}