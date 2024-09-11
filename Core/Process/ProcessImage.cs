using System.Collections.Frozen;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace QuadToSpine2D.Core.Process;

public class ProcessImage
{
    //skin tex_id layer_id layer_data
    public Dictionary<int, Dictionary<int, Dictionary<string, LayerData>?>> ImageData { get; }
    private readonly Dictionary<string, LayerData> _layerDataDict;
    public FrozenDictionary<string, LayerData> LayerDataDict { get; private set; }
    private int _skinsCount;
    private int _imageIndex;
    private int _currentImageIndex;
    private Image?[,] _images;

    public ProcessImage()
    {
        ImageData = [];
        _layerDataDict = [];
    }

    public void Process(List<List<string?>> imagesSrc, QuadJson quad)
    {
        Console.WriteLine("Cropping images...");
        GlobalData.LabelContent = "Cropping images...";

        _skinsCount = imagesSrc.Count;

        _images = new Image[imagesSrc.Count, imagesSrc[0].Count];
        GetAllImages(imagesSrc);
        foreach (var keyframe in quad.Keyframe)
        {
            InitImageData(keyframe);
        }

        LayerDataDict = _layerDataDict.ToFrozenDictionary();

        GlobalData.LabelContent = "Finish";
        Console.WriteLine("Finish");
    }

    private void InitImageData(Keyframe keyframe)
    {
        List<KeyframeLayer> layers = [];
        foreach (var layer in keyframe.Layer)
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

                var sameLayers = layers.Where(x => x.LayerGuid.Equals(layer.LayerGuid)).ToArray();
                for (int copyIndex = 0; copyIndex < sameLayers.Length; copyIndex++)
                {
                    // if (ImageData[curSkin][layer.TexId].TryGetValue(layer.LayerGuid, out var value) && copyIndex == 0)
                    // {
                    //     layer.LayerName = value.KeyframeLayer.LayerName;
                    //     continue;
                    // }

                    if (ImageNumData.TryGetValue(layer.LayerGuid, out var num))
                    {
                        if (num.TryGetValue(copyIndex, out var layerName))
                        {
                            layer.LayerName = layerName;
                            continue;
                        }
                    }
                    var layerData = CropImage(_images[curSkin, layer.TexId], rectangle, layer, curSkin, copyIndex);
                    if (layerData is null)
                    {
                        // Add fog... 
                        continue;
                    }
                    ImageData[curSkin][layer.TexId].Add(layer.LayerName, layerData);
                    _layerDataDict.Add(layer.LayerName, layerData);
                    if (!ImageNumData.TryGetValue(layer.LayerGuid, out var dict))
                    {
                        dict = [];
                        ImageNumData.Add(layer.LayerGuid, dict);
                    }
                    dict.Add(copyIndex,layer.LayerName);
                }
            }
        }
    }

    private Dictionary<string, Dictionary<int,string>> ImageNumData = [];
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
                    _images[i, j] = null;
                    ImageData[i][j] = null;
                    continue;
                }

                _images[i, j] = Image.Load(src);
                ImageData[i][j] = [];
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

    private LayerData? CropImage(Image? image, Rectangle rectangle, KeyframeLayer layer, int curSkin, int copyIndex)
    {
        if (image is null) return null;
        using var clipImage = image.Clone(x => { x.Crop(rectangle); });
        var imageName = $"Slice {_imageIndex}_{layer.TexId}_{curSkin}_{copyIndex}";
        if (ImageNumData.TryGetValue(layer.LayerGuid, out var dict))
        {
            imageName = dict[0].Remove(dict[0].Length - 1) + copyIndex;
        }
        _imageIndex++;
        layer.LayerName = imageName;
        clipImage.SaveAsPngAsync(Path.Combine(GlobalData.ImageSavePath, imageName + ".png"));
        _currentImageIndex++;
        layer.OrderId = _currentImageIndex;
        return new LayerData
        {
            ImageName = imageName,
            KeyframeLayer = layer,
        };
    }

    public Image DrawFogImage(int width, int height, string[] colors)
    {
        var image = new Image<Rgba32>(width, height);
        PointF[] fs =
        [
            new PointF(0, image.Width),
            new PointF(0, 0),
            new PointF(image.Height, 0),
            new PointF(image.Height, image.Width),
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