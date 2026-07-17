using System.Runtime.InteropServices;
using QTSAvalonia.Helper;
using SkiaSharp;
using VanillawareConverter.Ftex;

namespace QTSCore.Services;

/// <summary>
///     缩略图/PNG 生成服务：封装 SKBitmap 相关的像素拷贝与 PNG 编码逻辑。
/// </summary>
public class ThumbnailGenerator
{
    /// <summary>
    ///     将 <see cref="ImageResult" /> 编码为 PNG 并写入磁盘（原始分辨率，不缩放）。
    /// </summary>
    /// <remarks>导出原始分辨率，不缩放（Quad JSON 坐标引用原始尺寸）。</remarks>
    public void SaveAsPng(ImageResult result, string outputPath)
    {
        try
        {
            using var srcBitmap = new SKBitmap(result.Width, result.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

            var pixels = new byte[result.Width * result.Height * 4];
            for (var y = 0; y < result.Height; y++)
            for (var x = 0; x < result.Width; x++)
            {
                var dstOffset = (y * result.Width + x) * 4;
                var srcOffset = dstOffset;
                if (srcOffset + 3 < result.PixelData.Length)
                {
                    pixels[dstOffset] = result.PixelData[srcOffset];
                    pixels[dstOffset + 1] = result.PixelData[srcOffset + 1];
                    pixels[dstOffset + 2] = result.PixelData[srcOffset + 2];
                    pixels[dstOffset + 3] = result.PixelData[srcOffset + 3];
                }
            }

            Marshal.Copy(pixels, 0, srcBitmap.GetPixels(), pixels.Length);

            using var image = SKImage.FromBitmap(srcBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);

            File.WriteAllBytes(outputPath, data.ToArray());
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Failed to save PNG: {ex.Message}");
        }
    }
}