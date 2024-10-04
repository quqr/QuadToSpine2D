using System.Collections.Frozen;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace QuadToSpine2D.Core.Process;

public class ProcessImage
{
    private readonly Dictionary<string, LayerData> _layerDataDict = [];

    private readonly Dictionary<string, Dictionary<int, string>> ImageNumData = [];
    private          int                                         _currentImageIndex;
    private          Image?[,]                                   _images;
    private          int                                         _skinsCount;

    //skin tex_id layer_id layer_data
    public Dictionary<int, Dictionary<int, Dictionary<string, LayerData>?>> ImageData     { get; } = [];
    public FrozenDictionary<string, LayerData>                              LayerDataDict { get; private set; }

    public void Process(List<List<string?>> imagesSrc, QuadJsonData quad)
    {
        Console.WriteLine("Cropping images...");
        GlobalData.BarTextContent = "Cropping images...";

        _skinsCount = imagesSrc.Count;

        _images = new Image[imagesSrc.Count, imagesSrc[0].Count];
        GetAllImages(imagesSrc);
        // TODO : Need add animation track supports. Cause a full track may contain many keyframes
        foreach (var keyframe in quad.Keyframe) InitImageData(keyframe);

        GlobalData.BarValue = 50;

        LayerDataDict = _layerDataDict.ToFrozenDictionary();
    }

    private void InitImageData(Keyframe keyframe)
    {
        List<KeyframeLayer> layers = [];
        foreach (var layer in keyframe.Layers)
        {
            layers.Add(layer);
            if (layer.TexId > _images.Length)
                throw new ArgumentException($"Missing image. TexID: {layer.TexId}");

            var rectangle = CalculateRectangle(layer);
            for (var curSkin = 0; curSkin < _skinsCount; curSkin++)
            {
                //clip image.
                //if image continue to exist in next keyframe
                //Or if one keyframe has same layer, clip same image and rename.
                try
                {
                    if (ImageData[curSkin][layer.TexId] is null) continue;
                }
                catch (Exception)
                {
                    throw new ArgumentException($"Missing image. TexID: {layer.TexId}");
                }

                var sameLayers = layers.Where(x => x.Guid.Equals(layer.Guid)).ToArray();
                for (var copyIndex = 0; copyIndex < sameLayers.Length; copyIndex++)
                {
                    if (ImageNumData.TryGetValue(layer.Guid, out var num))
                        if (num.TryGetValue(copyIndex, out var layerName))
                        {
                            layer.LayerName = layerName;
                            continue;
                        }

                    LayerData? layerData;
                    if (layer.Srcquad is not null)
                    {
                        layerData = CropImage(_images[curSkin, layer.TexId], rectangle, layer, curSkin, copyIndex);
                    }
                    else
                    {
                        var fog = DrawFogImage(100, 100, layer.Fog);
                        layerData = CropImage(fog, new Rectangle(0, 0, 100, 100), layer, curSkin, copyIndex);
                    }

                    ImageData[curSkin][layer.TexId].Add(layer.LayerName, layerData);
                    _layerDataDict.Add(layer.LayerName, layerData);
                    if (!ImageNumData.TryGetValue(layer.Guid, out var dict))
                    {
                        dict = [];
                        ImageNumData.Add(layer.Guid, dict);
                    }

                    dict.Add(copyIndex, layer.LayerName);
                }
            }
        }
    }

    private void GetAllImages(List<List<string?>> images)
    {
        for (var i = 0; i < images.Count; i++)
        {
            ImageData[i] = [];
            for (var j = 0; j < images[0].Count; j++)
            {
                var src = images[i][j];
                if (src is null)
                {
                    _images[i, j]   = null;
                    ImageData[i][j] = null;
                    continue;
                }

                _images[i, j]   = Image.Load(src);
                ImageData[i][j] = [];
            }
        }
    }

    private Rectangle CalculateRectangle(KeyframeLayer layer)
    {
        return new Rectangle
        {
            X      = (int)layer.MinAndMaxSrcPoints[0],
            Y      = (int)layer.MinAndMaxSrcPoints[1],
            Width  = (int)layer.Width,
            Height = (int)layer.Height
        };
    }

    private LayerData? CropImage(Image? image, Rectangle rectangle, KeyframeLayer layer, int curSkin, int copyIndex)
    {
        if (image is null) return null;
        using var clipImage = image.Clone(x => { x.Crop(rectangle); });
        var       order     = _currentImageIndex;
        var       imageName = $"Slice_{_currentImageIndex}_{layer.TexId}_{curSkin}_{copyIndex}";
        if (ImageNumData.TryGetValue(layer.Guid, out var dict))
        {
            imageName = dict[0].Remove(dict[0].Length - 1) + copyIndex;
            order     = int.Parse(dict[0].Split("_")[1]);
        }
        else
        {
            _currentImageIndex++;
        }

        layer.LayerName = imageName;
        clipImage.SaveAsPngAsync(Path.Combine(GlobalData.ImageSavePath, imageName + ".png"));

        layer.ImageNameOrder = order * 1000 + layer.TexId * 100 + curSkin * 10 + copyIndex;

        return new LayerData
        {
            SlotAndImageName     = imageName,
            KeyframeLayer = layer,
            SkinIndex     = curSkin,
            ImageIndex    = _currentImageIndex,
            TexId         = layer.TexId,
            CopyIndex     = copyIndex
        };
    }

    private Image DrawFogImage(int width, int height, List<string> colors)
    {
        var image = new Image<Rgba32>(width, height);
        PointF[] fs =
        [
            new(0, image.Width),
            new(0, 0),
            new(image.Height, 0),
            new(image.Height, image.Width)
        ];
        Color[] cs =
        [
            Color.ParseHex(colors[0]),
            Color.ParseHex(colors[1]),
            Color.ParseHex(colors[2]),
            Color.ParseHex(colors[3])
        ];

        image.Mutate(x => { x.Fill(new PathGradientBrush(fs, cs)); });
        return image;
    }
}