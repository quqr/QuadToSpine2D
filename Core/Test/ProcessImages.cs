﻿using System.Collections.Concurrent;
using System.Threading.Tasks;
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
    private          int       _currentImageIndex;

    public ProcessImages(List<List<string?>> imagesSrc)
    {
        _skinsCount = imagesSrc[0].Count;
        _images     = new Image[imagesSrc.Count, _skinsCount];
        GetImages(imagesSrc);
    }

    // { tex_id: { skin_id: {layer_guid: [layer_data, ...] } }
    public ConcurrentDictionary<int, Dictionary<int, Dictionary<string, List<LayerData>>>> LayersDataDict { get; } = [];

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

    public List<LayerData> GetLayerData(KeyframeLayer layer, int copyIndex)
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
                data = CropImage(image, rectangle, layer, skinIndex, copyIndex);
            }
            else
            {
                data = CropImage(layer, skinIndex, copyIndex);
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

    private LayerData CropImage(Image image, Rectangle rectangle, KeyframeLayer layer, int curSkin, int copyIndex)
    {
        var imageName = $"Slice_{_currentImageIndex}_{layer.TexId}_{curSkin}_{copyIndex}";
        layer.OrderId = _currentImageIndex * 1000 + layer.TexId * 100 + curSkin * 10 + copyIndex;

        Task.Run(() =>
        {
            using var clipImage = image.Clone(x => { x.Crop(rectangle); });
            clipImage.SaveAsPngAsync(Path.Combine(GlobalData.ImageSavePath, $"{imageName}.png"));
        });

        return new LayerData
        {
            ImageName              = imageName,
            KeyframeLayer          = layer,
            SkinIndex              = curSkin,
            ImageIndex             = _currentImageIndex,
            TexId                  = layer.TexId,
            CopyIndex              = copyIndex,
            BaseSkinAttackmentName = $"Slice_{_currentImageIndex}_{layer.TexId}_0_{copyIndex}"
        };
    }

    private LayerData CropImage(KeyframeLayer layer, int curSkin, int copyIndex)
    {
        var imageName = $"Slice_{_currentImageIndex}_{layer.TexId}_{curSkin}_{copyIndex}";
        layer.OrderId = _currentImageIndex * 1000 + layer.TexId * 100 + curSkin * 10 + copyIndex;

        Task.Run(() =>
        {
            using var fog = DrawFogImage(100, 100, layer.Fog);
            fog.SaveAsPngAsync(Path.Combine(GlobalData.ImageSavePath, $"{imageName}.png"));
        });

        return new LayerData
        {
            ImageName              = imageName,
            KeyframeLayer          = layer,
            SkinIndex              = curSkin,
            ImageIndex             = _currentImageIndex,
            TexId                  = layer.TexId,
            CopyIndex              = copyIndex,
            BaseSkinAttackmentName = $"Slice_{_currentImageIndex}_{layer.TexId}_0_{copyIndex}"
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