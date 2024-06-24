using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace QuadPlayer.Process;

public class ProcessImage
{
    public readonly Dictionary<int, Dictionary<string, LayerData>> ImagesData = new();
    public string SavePath;
    public int SkinsCount;
    private int _imageIndex;
    private Image[,] _images;
    private bool _isCopy;
    public void Process(List<List<string>> imagesSrc, QuadJson quad, string savePath)
    {
        Console.WriteLine("Clipping images...");

        SkinsCount = imagesSrc.Count;

        _images = new Image[imagesSrc.Count, imagesSrc[0].Count];
        SavePath = savePath;
        GetAllImages(imagesSrc);
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
                var rectangle = CalculateRectangle(layer);
                for (int curSkin = 0; curSkin < SkinsCount; curSkin++)
                {
                    ImagesData.TryAdd(curSkin, new Dictionary<string, LayerData>());
                    //clip image.
                    //if image exist continue.
                    //Or if one keyframe has same layer, clip same image and rename.
                    if (ImagesData[curSkin].TryGetValue(layer.LayerGuid, out var value))
                    {
                        layer.LayerName = curSkin == 0 ? value.ImageName : layer.LayerName;
                        var layerCount = layers.Count(x => x.LayerGuid.Equals(layer.LayerGuid));
                        if (layerCount < 2) continue;
                        layer.LayerName = curSkin == 0 ? $"{layer.LayerName}_COPY_{layerCount}" : layer.LayerName;
                        layer.LayerGuid = curSkin == 0 ? $"{layer.LayerGuid}_COPY_{layerCount}" : layer.LayerGuid;
                        if (ImagesData[curSkin].ContainsKey(layer.LayerGuid)) continue;
                        _isCopy = true;
                    }
                    ImagesData[curSkin][layer.LayerGuid] = ClipImage(_images[curSkin, layer.TexID], rectangle, layer, curSkin);
                }

            }
        }
        Console.WriteLine("Finish");
    }
    private void GetAllImages(List<List<string>> images)
    {
        for (int i = 0; i < images.Count; i++)
        {
            for (int j = 0; j < images[0].Count; j++)
            {
                _images[i, j] = Image.Load(images[i][j]);
            }
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
    private LayerData ClipImage(Image image, Rectangle rectangle, KeyframeLayer layer, int curSkin)
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
            imageName = $"Slice {_imageIndex}_{layer.TexID}_{curSkin}";
            _imageIndex++;
        }
        if (curSkin == 0)
        {
            layer.LayerName = imageName;
        }
        clipImage.SaveAsPngAsync(Path.Combine(SavePath, imageName + ".png"));
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
    public float[] ZeroCenterPoints { get; set; } = new float[8];
}