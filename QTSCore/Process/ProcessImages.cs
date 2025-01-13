using System.Collections.Concurrent;
using System.Threading.Tasks;
using Avalonia.Threading;
using QuadToSpine2D.Core.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace QuadToSpine2D.Core.Process;

public class ProcessImages
{
    private readonly Image?[,] _images;
    private readonly int       _skinsCount;

    public ProcessImages(List<List<string?>> imagesSrc)
    {
        _skinsCount = imagesSrc[0].Count;
        _images     = new Image[imagesSrc.Count, _skinsCount];
        GetImages(imagesSrc);
    }

    private int _currentImageIndex { get; set; }

    // { tex_id: { skin_id: {layer_guid: [layer_data, ...] } }
    private ConcurrentDictionary<int, Dictionary<int, Dictionary<string, List<LayerData>>>> LayersDataDict { get; } = [];

    private void GetImages(List<List<string?>> images)
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

    public List<LayerData> GetLayerData(KeyframeLayer layer, PoolData? poolData, int copyIndex)
    {
        var layersData = new List<LayerData>();
        for (var skinIndex = 0; skinIndex < _skinsCount; skinIndex++)
        {
            LayerData data;
            if (layer.Srcquad is not null)
            {
                var image = _images[layer.TexId, skinIndex];
                if (image is null)
                    continue;
                var rectangle = ProcessUtility.CalculateRectangle(layer);
                data = CropImage(image, rectangle, layer, poolData, skinIndex, copyIndex);
            }
            else
            {
                data = CropImage(layer, poolData, skinIndex, copyIndex);
            }

            if (!LayersDataDict.ContainsKey(layer.TexId))
                LayersDataDict[layer.TexId] = [];
            if (!LayersDataDict[layer.TexId].TryGetValue(skinIndex, out _))
                LayersDataDict[layer.TexId][skinIndex] = [];
            if (!LayersDataDict[layer.TexId][skinIndex].TryGetValue(layer.Guid, out var curLayerData))
            {
                curLayerData                                       = [];
                LayersDataDict[layer.TexId][skinIndex][layer.Guid] = curLayerData;
            }

            curLayerData.Add(data);
            layersData.Add(data);
        }

        _currentImageIndex = copyIndex == 0 ? _currentImageIndex + 1 : _currentImageIndex;
        return layersData;
    }

    private LayerData CropImage(Image image, Rectangle rectangle, KeyframeLayer layer, PoolData? poolData, int curSkin,
        int                           copyIndex)
    {
        var imageName = GerImageName(layer, poolData, curSkin, copyIndex, out var imageIndex);


        Task.Run(() =>
        {
            using var clipImage = image.Clone(x => { x.Crop(rectangle); });
            clipImage.SaveAsPngAsync(Path.Combine(GlobalData.ImageSavePath, $"{imageName}.png"));
        }).ContinueWith(CatchErrors);

        return new LayerData
        {
            SlotAndImageName       = imageName,
            KeyframeLayer          = layer,
            SkinIndex              = curSkin,
            ImageIndex             = imageIndex,
            TexId                  = layer.TexId.ToString(),
            CopyIndex              = copyIndex,
            BaseSkinAttachmentName = $"Slice_{imageIndex}_{layer.TexId}_0_{copyIndex}"
        };
    }

    private static void CatchErrors(Task task)
    {
        if (task.Exception?.InnerException is null) return;
        Console.WriteLine(task.Exception.InnerException.Message);
        GlobalData.IsCompleted = false;
        Dispatcher.UIThread.Post(() =>
        {
            GlobalData.BarValue              = 100;
            GlobalData.ProcessBar.Foreground = GlobalData.ProcessBarErrorBrush;
            GlobalData.BarTextContent        = task.Exception.InnerException.Message;
        });
    }

    private string GerImageName(KeyframeLayer layer, PoolData? poolData, int curSkin, int copyIndex, out int imageIndex)
    {
        imageIndex = poolData?.LayersData[curSkin].ImageIndex ?? _currentImageIndex;
        var isFog     = layer.TexId == GlobalData.FogTexId ? "Fog" : layer.TexId.ToString();
        var imageName = $"Slice_{imageIndex}_{isFog}_{curSkin}_{copyIndex}";
        layer.ImageNameOrder = imageIndex * 1000 + layer.TexId * 100 + curSkin * 10 + copyIndex;
        return imageName;
    }

    private LayerData CropImage(KeyframeLayer layer, PoolData? poolData, int curSkin, int copyIndex)
    {
        var imageName = GerImageName(layer, poolData, curSkin, copyIndex, out var imageIndex);

        Task.Run(() =>
        {
            using var fog = DrawFogImage(100, 100, layer.Fog);
            fog.SaveAsPngAsync(Path.Combine(GlobalData.ImageSavePath, $"{imageName}.png"));
        }).ContinueWith(CatchErrors);

        return new LayerData
        {
            SlotAndImageName = imageName,
            KeyframeLayer    = layer,
            SkinIndex        = curSkin,
            ImageIndex       = imageIndex,
            CopyIndex        = copyIndex,
            TexId            = layer.TexId.ToString()
        };
    }

    private static Image<Rgba32> DrawFogImage(int width, int height, List<string> colors)
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