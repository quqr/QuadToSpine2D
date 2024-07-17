using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace QuadToSpine2D.Tools;

public static class ImageLoader
{
    public static Bitmap LoadImage(string path)
    {
        using var file = File.Open(path, FileMode.Open);
        return new Bitmap(file);
    }

    public static Bitmap LoadImage(IStorageFile file)
    {
        var path = Utility.ConvertUriToPath(file.Path);
        return LoadImage(path);
    }
}