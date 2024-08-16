using System.Web;
using Avalonia.Platform.Storage;

namespace QuadToSpine2D.MyUtility;

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

    private static readonly FilePickerFileType QuadFileTypeFilter = new("quad") { Patterns = ["*.quad"] };
    public static IReadOnlyList<IStorageFile>? OpenQuadFilePicker(IStorageProvider storageProvider)
    {
        var files = storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "QuadFile",
            SuggestedStartLocation = null,
            AllowMultiple = false,
            FileTypeFilter = [QuadFileTypeFilter]
        }).Result;
        return files.Count == 0 ? null : files;
    }
    
    public static string ConvertUriToPath(Uri uri)
    {
        return HttpUtility.UrlDecode(uri.AbsolutePath);
    }
    /// <summary>
    /// Flip image path to right format
    /// </summary>
    /// <param name="imagePath"></param>
    /// <returns></returns>
    public static List<List<string?>> ConvertImagePath(List<List<string?>?> imagePath)
    {
        List<List<string?>> result = [];
        var nullCount = imagePath.Count(x => x is null || x.Count == 0);
        if (imagePath.Count - nullCount == 0) return result;
        imagePath.RemoveAll(x => x is null || x.Count == 0);
        var maxCount = imagePath.MaxBy(x => x?.Count)?.Count;
        foreach (var path in imagePath)
            for (var j = 0; j < maxCount; j++)
            {
                if (result.Count < maxCount) result.Add([]);
                if (j >= path.Count)
                {
                    result[j].Add(null);
                    continue;
                }

                result[j].Add(path[j]);
            }

        return result;
    }
}