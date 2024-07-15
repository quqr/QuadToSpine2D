using QuadToSpine.Data;
using QuadToSpine.Data.Quad;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace QuadToSpine.Process;

public class ProcessImage
{
    //skin tex_id layer_id layer_data
    public Dictionary<int, Dictionary<int, Dictionary<string, LayerData>?>> ImageData { get; } = new();
    public Dictionary<string, LayerData> LayerDataDict{ get; } = new();
    public string SavePath { get; private set; }
    private int _skinsCount;
    private int _imageIndex;
    private int _currentImageIndex;
    private Image?[,] _images;
    private bool IsCopy{ get; set; }

    public void Process(List<List<string?>> imagesSrc, QuadJson quad, string savePath)
    {
        Console.WriteLine("Cropping images...");

        _skinsCount = imagesSrc.Count;
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
                if (layer.TexId > _images.Length)
                {
                    Console.WriteLine($"Missing image. TexID: {layer.TexId}");
                    continue;
                }

                var rectangle = CalculateRectangle(layer);
                for (var curSkin = 0; curSkin < _skinsCount; curSkin++)
                {
                    //ImageLayerData.TryAdd(curSkin, new Dictionary<string, LayerData?>());
                    //clip image.
                    //if image exist continue.
                    //Or if one keyframe has same layer, clip same image and rename.
                    if(ImageData[curSkin][layer.TexId] is null)continue;
                    if (ImageData[curSkin][layer.TexId].TryGetValue(layer.LayerGuid,out var value))
                    {
                        //if (ImageLayerData[curSkin].TryGetValue(layer.LayerGuid, out var value))
                        //if(value is null) continue;
                        layer.LayerName = curSkin == 0 ? value.ImageName : layer.LayerName;
                        var layerCount = layers.Count(x => x.LayerGuid.Equals(layer.LayerGuid));
                        if (layerCount < 2) continue;
                        layer.LayerName = curSkin == 0 ? $"{layer.LayerName}_COPY_{layerCount}" : layer.LayerName;
                        layer.LayerGuid = curSkin == 0 ? $"{layer.LayerGuid}_COPY_{layerCount}" : layer.LayerGuid;
                        //if (ImageLayerData[curSkin].ContainsKey(layer.LayerGuid)) continue;
                        if (ImageData[curSkin][layer.TexId].ContainsKey(layer.LayerGuid)) continue;
                        IsCopy = true;
                    }
                    var layerData = ClipImage(_images[curSkin, layer.TexId], rectangle, layer, curSkin);
                    ImageData[curSkin][layer.TexId].TryAdd(layer.LayerGuid, layerData);
                    LayerDataDict.TryAdd(layer.LayerGuid, layerData);
                }
            }
        }
        Console.WriteLine("Finish");
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
        if (IsCopy)
        {
            imageName = layer.LayerName;
            IsCopy = false;
        }
        else
        {
            imageName = $"Slice {_imageIndex}_{layer.TexId}_{curSkin}";
            _imageIndex++;
        }
        if (curSkin == 0) layer.LayerName = imageName;
        clipImage.SaveAsPngAsync(Path.Combine(SavePath, imageName + ".png"));
        _currentImageIndex++;
        return new LayerData
        {
            UVs = layer.UVs,
            ImageName = imageName,
            ZeroCenterPoints = layer.ZeroCenterPoints,
            KeyframeLayer = layer,
            CurrentImageIndex = _currentImageIndex
        };
    }
}