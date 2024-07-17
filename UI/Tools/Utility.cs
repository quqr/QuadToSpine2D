using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace UI.Tools;

public static class Utility
{
    public static IReadOnlyList<IStorageFile>? OpenImageFilePicker(IStorageProvider storageProvider)
    {
        var files = storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Images",
            SuggestedStartLocation = null,
            AllowMultiple = true,
            FileTypeFilter = [ FilePickerFileTypes.ImageAll ]
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
        int maxCount = imagePath.MaxBy(x => x.Count).Count;
        for (int i = 0; i < imagePath.Count; i++)
        {
            for (int j = 0; j < maxCount; j++)
            {
                if (result.Count < maxCount) result.Add([]);
                if (j > imagePath[i].Count)
                {
                    result[j].Add(null);
                    continue;
                }
                result[j].Add(imagePath[i][j]);
            }
        }

        return result;
    }
}