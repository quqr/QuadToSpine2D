using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
namespace QuadPlayer;
using SixLabors.ImageSharp;

public class ProcessImage
{
    public Dictionary<string, LayerData> ImagesData { get; set; }= new();
    public string SavePath;
    private int _imageIndex;
    private Image[] _images;
    public ProcessImage(List<string> images,QuadJson quad,string savePath)
    {
        Console.WriteLine("Cutting images...");
        _images = new Image[images.Count];
        SavePath = savePath;
        GetAllImages(images);
        var layers = quad.Keyframe.SelectMany(keyframe => keyframe.Layer);
        foreach (var layer in layers)
        {
            if (ImagesData.TryGetValue(layer.LayerGuid, out var value))
            {
                layer.LayerName = value.ImageName;
                continue;
            }
            ImagesData[layer.LayerGuid] = CutImage(_images[layer.TexID], CalculateRectangle(layer), layer);
        }
        DisposeImages();
        Console.WriteLine("Finish");
    }

    private void DisposeImages()
    {
        foreach (var i in _images)
        {
            i.Dispose();
        }
    }

    private void GetAllImages(List<string> images)
    {
        for (var index = 0; index < images.Count; index++)
        {
            var image = Image.Load(images[index]);
            _images[index] = image;
        }
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

    private LayerData CutImage(Image image,Rectangle rectangle,KeyframeLayer layer)
    {
        using var cutImage = image.Clone(x =>
        {
            x.Crop(rectangle);
        });
        
        var imageName = $"Slice {layer.TexID}_{_imageIndex}";
        layer.LayerName = imageName;
        cutImage.SaveAsPng($"{SavePath}\\{imageName}.png");
        _imageIndex++;
        return new LayerData
        {
            UVs = layer.UVs,
            ImageName = imageName,
            ZeroCenterPoints = layer.ZeroCenterPoints,
        };
    }
}

public class LayerData
{
    public float[] UVs { get; set; } = new float[8];
    public string ImageName { get; set; } = string.Empty;
    public float[] ZeroCenterPoints { get; set; }= new float[8];
}