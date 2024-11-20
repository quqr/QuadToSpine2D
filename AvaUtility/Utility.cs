using System.Web;
using Avalonia.Platform.Storage;

namespace QuadToSpine2D.AvaUtility;

public static class Utility
{
    private static readonly FilePickerFileType QuadFileTypeFilter = new("quad") { Patterns = ["*.quad"] };

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
            FileTypeFilter = [QuadFileTypeFilter]
        }).Result;
        return files.Count == 0 ? null : files;
    }

    /// <summary>
    ///     Flip image path to right format
    /// </summary>
    /// <param name="imagePath"></param>
    /// <returns></returns>
    public static List<List<string?>> ConvertImagePath(List<List<string?>?> imagePath)
    {
        List<List<string?>> result = [];
        var temp = CopyList(imagePath);
        if (temp.Count == 0) return [];
        var maxCount = temp.MaxBy(x => x.Count)?.Count;
        if (maxCount is null) return [];
        foreach (var path in temp)
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

    private static List<List<string>> CopyList(List<List<string?>?> list)
    {
        var newList = new List<List<string>>();
        foreach (var i in list)
        {
            if (i is null) continue;
            newList.Add([]);
            foreach (var j in i)
            {
                if (j is null) continue;
                newList[^1].Add(j);
            }
        }

        return newList;
    }

    public static string DecodePath(this Uri uri)
    {
        return HttpUtility.UrlDecode(uri.AbsolutePath);
    }
}