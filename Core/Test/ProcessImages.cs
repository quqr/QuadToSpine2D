using QuadToSpine2D.Core.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace QuadToSpine2D.Core.Process;

public class ProcessImages
{
    // { tex_id: { layer_guid: pool_data } }
    // pool_data: { skin_id: [layer_data] };
    public Dictionary<int, Dictionary<string, PoolData>> LayersDataDict { get; } = [];
    private int _currentImageIndex;
    private readonly int _skinsCount;
    private readonly Image?[,] _images;
    public ProcessImages(List<List<string?>> imagesSrc)
    {
        _skinsCount = imagesSrc[0].Count;
        _images = new Image[imagesSrc.Count, _skinsCount];
        GetAllImages(imagesSrc);
    }

    private void GetAllImages(List<List<string?>> images)
    {
        for (var i = 0; i < images.Count; i++)
        {
            for (var j = 0; j < _skinsCount; j++)
            {
                var src = images[i][j];
                if (src is null)
                {
                    _images[i, j] = null;
                    continue;
                }
                _images[i, j] = Image.Load(src);
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

    public PoolData GetLayerData(KeyframeLayer layer, int copyIndex)
    {
        for (var skinIndex = 0; skinIndex < _skinsCount; skinIndex++)
        {
            var rectangle = CalculateRectangle(layer);
            var image = _images[layer.TexId, skinIndex];
            if (image is null) 
                continue;
            var data = CropImage(image, rectangle, layer, skinIndex, copyIndex);
            
            if(!LayersDataDict.ContainsKey(layer.TexId))
                LayersDataDict[layer.TexId] = [];
            if (!LayersDataDict[layer.TexId].TryGetValue(layer.LayerGuid, out var poolData))
            {
                poolData = new PoolData();
                LayersDataDict[layer.TexId][layer.LayerGuid] = poolData;
            }

            if (!poolData.LayersData.TryGetValue(skinIndex, out var layerData))
            {
                layerData = [];
                poolData.LayersData[skinIndex] = layerData;
            }
            layerData.Add(data);
        }

        _currentImageIndex = copyIndex == 0 ? _currentImageIndex + 1 : _currentImageIndex;
        return LayersDataDict[layer.TexId][layer.LayerGuid];
    }

    private LayerData CropImage(Image image, Rectangle rectangle, KeyframeLayer layer, int curSkin, int copyIndex)
    {
        using var clipImage = image.Clone(x => { x.Crop(rectangle); });
        var imageName = $"Slice_{_currentImageIndex}_{layer.TexId}_{curSkin}_{copyIndex}";
        clipImage.SaveAsPngAsync(Path.Combine(GlobalData.ImageSavePath, $"{imageName}.png"));
        layer.OrderId = _currentImageIndex * 1000 + layer.TexId * 100 + curSkin * 10 + copyIndex;

        return new LayerData
        {
            ImageName = imageName,
            KeyframeLayer = layer,
            SkinIndex = curSkin,
            ImageIndex = _currentImageIndex,
            TexId = layer.TexId,
            CopyIndex = copyIndex,
            ImageNameData = [_currentImageIndex, curSkin, layer.TexId, copyIndex],
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