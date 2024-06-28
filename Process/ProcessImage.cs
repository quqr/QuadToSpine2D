using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace QuadToSpine.Process;

public class ProcessImage
{
    //private Dictionary<int, Dictionary<string, LayerData?>> ImageLayerData { get; } = new();
    public Dictionary<int, Dictionary<int, Dictionary<string, LayerData>?>> ImageData { get; } = new();
    public string SavePath{ get; private set; }
    private int SkinsCount{ get; set; }
    private int _imageIndex;
    private Image?[,] _images;
    private bool _isCopy;

    public void Process(List<List<string?>> imagesSrc, QuadJson quad, string savePath)
    {
        Console.WriteLine("Clipping images...");

        SkinsCount = imagesSrc.Count;
        //SortImagesSrc(imagesSrc);
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
                for (var curSkin = 0; curSkin < SkinsCount; curSkin++)
                {
                    //ImageLayerData.TryAdd(curSkin, new Dictionary<string, LayerData?>());
                    //clip image.
                    //if image exist continue.
                    //Or if one keyframe has same layer, clip same image and rename.
                    if(ImageData[curSkin][layer.TexID] is null)continue;
                    if (ImageData[curSkin][layer.TexID].TryGetValue(layer.LayerGuid,out var value))
                    {
                        //if (ImageLayerData[curSkin].TryGetValue(layer.LayerGuid, out var value))
                        //if(value is null) continue;
                        layer.LayerName = curSkin == 0 ? value.ImageName : layer.LayerName;
                        var layerCount = layers.Count(x => x.LayerGuid.Equals(layer.LayerGuid));
                        if (layerCount < 2) continue;
                        layer.LayerName = curSkin == 0 ? $"{layer.LayerName}_COPY_{layerCount}" : layer.LayerName;
                        layer.LayerGuid = curSkin == 0 ? $"{layer.LayerGuid}_COPY_{layerCount}" : layer.LayerGuid;
                        //if (ImageLayerData[curSkin].ContainsKey(layer.LayerGuid)) continue;
                        if (ImageData[curSkin][layer.TexID].ContainsKey(layer.LayerGuid)) continue;
                        _isCopy = true;
                    }
                    var layerData = ClipImage(_images[curSkin, layer.TexID], rectangle, layer, curSkin);
                    //ImageLayerData[curSkin][layer.LayerGuid] = layerData;
                    ImageData[curSkin][layer.TexID]?.TryAdd(layer.LayerGuid, layerData);
                }
            }
        }

        Console.WriteLine("Finish");
    }

    private void SortImagesSrc(List<List<string?>> imagesSrc)
    {
        foreach (var list in imagesSrc)
        {
            list.Sort();
        }
    }

    private void GetAllImages(List<List<string?>> images)
    {
        for (var i = 0; i < images.Count; i++)
        {
            ImageData[i] = new Dictionary<int, Dictionary<string, LayerData>?>();
            for (var j = 0; j < images[0].Count; j++)
            {
                var src = images[i][j];
                if (src is null)
                {
                    _images[i, j] = null;
                    ImageData[i][j] = null;
                    continue;
                }
                _images[i, j] = Image.Load(src);
                ImageData[i][j] = new Dictionary<string, LayerData>();
            }
        }
    }

    private Rectangle CalculateRectangle(KeyframeLayer layer)
    {
        return new Rectangle
        {
            X = (int)layer.MinAndMaxSrcPoints[0],
            Y = (int)layer.MinAndMaxSrcPoints[1],
            Width = (int)layer.Width,
            Height = (int)layer.Height
        };
    }

    private LayerData? ClipImage(Image? image, Rectangle rectangle, KeyframeLayer layer, int curSkin)
    {
        if (image is null) return null;
        using var clipImage = image.Clone(x =>
        {
            x.Crop(rectangle);
        });
        string imageName;
        if (_isCopy)
        {
            imageName = layer.LayerName;
            _isCopy = false;
        }
        else
        {
            imageName = $"Slice {_imageIndex}_{layer.TexID}_{curSkin}";
            //imageName = $"skins_{curSkin}/skin_{layer.TexID}";
            _imageIndex++;
        }

        if (curSkin == 0) layer.LayerName = imageName;
        clipImage.SaveAsPngAsync(Path.Combine(SavePath, imageName + ".png"));
        return new LayerData
        {
            UVs = layer.UVs,
            ImageName = imageName,
            ZeroCenterPoints = layer.ZeroCenterPoints
        };
    }
}

public class LayerData
{
    public float[] UVs { get; init; } = new float[8];
    public string ImageName { get; init; } = string.Empty;
    public float[] ZeroCenterPoints { get; init; } = new float[8];
}