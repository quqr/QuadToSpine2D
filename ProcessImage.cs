using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
namespace QuadPlayer;
using SixLabors.ImageSharp;

public class ProcessImage: Singleton<ProcessImage>
{
    public Dictionary<string, LayerData> ImagesData { get; } = new();
    public string SavePath;
    private int _imageIndex;
    private Image<Rgba32>[] _images;
    private bool _isCopy;
    public void Process(List<string> images, QuadJson quad, string savePath)
    {
        Console.WriteLine("Clipping images...");
        _images = new Image<Rgba32>[images.Count];
        SavePath = savePath;
        GetAllImages(images);
        foreach (var keyframe in quad.Keyframe)
        {
            List<KeyframeLayer> layers = [];
            foreach (var layer in keyframe.Layer)
            {
                layers.Add(layer);
                if (layer.TexID > _images.Length)
                {
                    Console.WriteLine($"Missing image. TexID: {layer.TexID}");
                    continue;
                }
                //clip image.
                //if image exist continue.
                //Or if one keyframe has same layer, clip same image and rename.
                if (ImagesData.TryGetValue(layer.LayerGuid, out var value))
                {
                    layer.LayerName = value.ImageName;
                    var layerCount = layers.Count(x => x.LayerGuid.Equals(layer.LayerGuid));
                    if (layerCount < 2) continue;
                    layer.LayerName += $"_COPY_{layerCount}";
                    layer.LayerGuid += $"_COPY_{layerCount}";
                    if (ImagesData.ContainsKey(layer.LayerGuid)) continue;
                    _isCopy = true;
                }
                
                ImagesData[layer.LayerGuid] = ClipImage(_images[layer.TexID], CalculateRectangle(layer), layer);
            }
        }
        Console.WriteLine("Finish");

    }
    private void GetAllImages(List<string> images)
    {
        for (var index = 0; index < images.Count; index++)
        {
            _images[index] = Image.Load<Rgba32>(images[index]);
        }
    }

    private Rectangle CalculateRectangle(KeyframeLayer layer)
    {
        return new Rectangle()
        {
            X = (int)layer.MinAndMaxSrcPoints[0],
            Y = (int)layer.MinAndMaxSrcPoints[1],
            Width = (int)layer.Width,
            Height = (int)layer.Height
        };
    }
    private LayerData ClipImage(Image<Rgba32> image, Rectangle rectangle, KeyframeLayer layer)
    {
        using var clipImage = image.Clone(x => { x.Crop(rectangle); });
        string imageName;
        if (_isCopy)
        {
            imageName = layer.LayerName;
            _isCopy = false;
        }
        else
        {
            imageName = $"Slice {layer.TexID}_{_imageIndex}_0";
            _imageIndex++;
        }
        layer.LayerName = imageName;
        clipImage.SaveAsPngAsync($"{SavePath}\\{imageName}.png");
        return new LayerData
        {
            UVs = layer.UVs,
            ImageName = imageName,
            ZeroCenterPoints = layer.ZeroCenterPoints,
            KeyframeLayer = layer
        };
    }
}

public class LayerData
{
    public float[] UVs { get; set; } = new float[8];
    public string ImageName { get; set; } = string.Empty;
    public float[] ZeroCenterPoints { get; set; } = new float[8];
    public KeyframeLayer KeyframeLayer { get; set; }
}