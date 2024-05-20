using SixLabors.ImageSharp.Processing;

namespace QuadPlayer;
using SixLabors.ImageSharp;

public class ProcessImage
{
    public Dictionary<string, Image> ClipImages = new();
    public ProcessImage(string imagePath,QuadJson quad)
    {
        Image image = ReadImage(imagePath);
        foreach (var keyframe in quad.Keyframe)
        {
            if(keyframe?.Layer is null) continue;
            foreach (var layer in keyframe.Layer)
            {
                if (layer is null ||
                    layer.LayerGuid.Equals(string.Empty) ||
                    ClipImages.ContainsKey(layer.LayerGuid) ||
                    layer.Srcquad is null) continue;
                ClipImages[layer.LayerGuid] = CutImage(image,CalculateRectangle(layer));
            }
        }
        Console.WriteLine("ProcessImage Finished");
    }
    private Rectangle CalculateRectangle(KeyframeLayer layer)
    {
        return new Rectangle()
        {
            X = (int)layer.MinAndMaxSrcPoints[0],
            Y = (int)layer.MinAndMaxSrcPoints[1],
            Width = (int)layer.Width,
            Height = (int)layer.Height,
        };
    }
    private Image ReadImage(string src)
    {
        return Image.Load(src);
    }

    private int _imageIndex=0;
    private Image CutImage(Image image,Rectangle rectangle)
    {
        var cutImage = image.Clone(context =>
        {
            context.Crop(rectangle);
        });
        cutImage.SaveAsPng($"D:\\Download\\quad_mobile_v05_beta-20240404-2000\\quad_mobile_v05_beta\\data\\Output\\{_imageIndex}.png");
        _imageIndex++;
        return cutImage;
    }
}