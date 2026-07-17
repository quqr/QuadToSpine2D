using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace QTSAvalonia.Helper;

public static class Utility
{
}

internal static class SkiaExtensions
{
    public static SKBitmap? ToSKBitmap(this Stream? stream)
    {
        return stream == null ? null : SKBitmap.Decode(stream);
    }

    public static IImage? ToAvaloniaImage(this SKBitmap? bitmap)
    {
        return bitmap is not null ? new AvaloniaImage(bitmap) : null;
    }

    public static IImage? ToAvaloniaImage(this SKImage? image)
    {
        return image is not null ? new AvaloniaImage(image) : null;
    }

    private record SKBitmapDrawOperation : ICustomDrawOperation
    {
        public SKBitmap? Bitmap { get; init; }
        public Rect Bounds { get; set; }

        public void Dispose()
        {
            Bitmap?.Dispose();
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return false;
        }

        public bool HitTest(Point p)
        {
            return Bounds.Contains(p);
        }

        public void Render(ImmediateDrawingContext context)
        {
            if (Bitmap != null && context.PlatformImpl.GetFeature<ISkiaSharpApiLeaseFeature>() is { } leaseFeature)
            {
                using var lease = leaseFeature.Lease();
                lease.SkCanvas.DrawBitmap(Bitmap,
                    SKRect.Create((float)Bounds.X, (float)Bounds.Y, (float)Bounds.Width, (float)Bounds.Height));
            }
        }
    }

    private record SKImageDrawOperation : ICustomDrawOperation
    {
        public SKImage? Image { get; init; }
        public Rect Bounds { get; set; }

        public void Dispose()
        {
            Image?.Dispose();
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return false;
        }

        public bool HitTest(Point p)
        {
            return Bounds.Contains(p);
        }

        public void Render(ImmediateDrawingContext context)
        {
            if (Image != null && context.PlatformImpl.GetFeature<ISkiaSharpApiLeaseFeature>() is { } leaseFeature)
            {
                using var lease = leaseFeature.Lease();
                lease.SkCanvas.DrawImage(Image,
                    SKRect.Create((float)Bounds.X, (float)Bounds.Y, (float)Bounds.Width, (float)Bounds.Height));
            }
        }
    }

    private class AvaloniaImage : IImage, IDisposable
    {
        private readonly SKObject? _source;
        private SKBitmapDrawOperation? _drawBitmapOperation;
        private SKImageDrawOperation? _drawImageOperation;

        public AvaloniaImage(SKBitmap? source)
        {
            _source = source;
            if (source?.Info.Size is { } size) Size = new Size(size.Width, size.Height);
        }

        public AvaloniaImage(SKImage? source)
        {
            _source = source;
            if (source?.Info.Size is { } size) Size = new Size(size.Width, size.Height);
        }

        public void Dispose()
        {
            _source?.Dispose();
        }

        public Size Size { get; }

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
            switch (_source)
            {
                case SKBitmap bitmap:
                    _drawBitmapOperation ??= new SKBitmapDrawOperation
                    {
                        Bitmap = bitmap
                    };
                    _drawBitmapOperation.Bounds = destRect;
                    context.Custom(_drawBitmapOperation);

                    break;
                case SKImage image:
                    _drawImageOperation ??= new SKImageDrawOperation
                    {
                        Image = image
                    };
                    _drawImageOperation.Bounds = destRect;
                    context.Custom(_drawImageOperation);

                    break;
                default:
                    return;
            }
        }
    }
}