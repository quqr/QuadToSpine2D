using System.Diagnostics;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace QTSAvalonia.Helper;

public static class Utility
{
    public static Bitmap ToBitmap(this SKBitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return new Bitmap(ms);
    }
}