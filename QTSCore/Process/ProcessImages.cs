using System.Collections.Concurrent;
using System.Diagnostics;
using QTSAvalonia.Helper;
using QTSAvalonia.ViewModels.Pages;
using QTSCore.Data;
using QTSCore.Data.Quad;
using QTSCore.Utility;
using SkiaSharp;

namespace QTSCore.Process;

public class ProcessImages
{
    private readonly SKBitmap?[,] _images;

    // Global lock: only for protecting _currentImageIndex updates (lightweight)
    private readonly Lock _indexLock = new();

    // Thread-safe nested dictionary: TexId -> SkinIndex -> Guid -> LayerData list
    private readonly ConcurrentDictionary<int,
        ConcurrentDictionary<int,
            ConcurrentDictionary<string, ConcurrentBag<LayerData>>>> _layersDataDict
        = new();

    private readonly int _skinsCount;
    private int _currentImageIndex;

    /// <summary>
    ///     Initialize image resource library
    /// </summary>
    public ProcessImages(List<List<string?>> imagesSrc)
    {
        LoggerHelper.Info($"Initializing image resource library, source count: {imagesSrc?.Count ?? 0}");

        if (imagesSrc == null || imagesSrc.Count == 0 || imagesSrc[0].Count == 0)
        {
            const string errorMsg = "Image source list cannot be empty";
            LoggerHelper.Error(errorMsg);
            throw new ArgumentException(errorMsg);
        }

        _skinsCount = imagesSrc[0].Count;
        _images = new SKBitmap[imagesSrc.Count, _skinsCount];

        LoggerHelper.Debug(
            $"Image configuration: {_images.GetLength(0)} x {_images.GetLength(1)}, skin count: {_skinsCount}");
        LoadImagesParallel(imagesSrc);

        LoggerHelper.Info("Image resource library initialization completed");
    }

    /// <summary>
    ///     Parallel load all texture images (thread-safe)
    /// </summary>
    private void LoadImagesParallel(List<List<string?>> imagesSrc)
    {
        LoggerHelper.Info($"Starting parallel loading of {_images.GetLength(0)} texture images");

        var failedCount = 0;
        var successCount = 0;
        var stopwatch = Stopwatch.StartNew();

        Parallel.For(0, imagesSrc.Count, i =>
        {
            for (var j = 0; j < _skinsCount; j++)
            {
                var path = imagesSrc[i][j];
                if (string.IsNullOrWhiteSpace(path))
                {
                    _images[i, j] = null;
                    Interlocked.Increment(ref failedCount);
                    LoggerHelper.Debug($"Skipping empty path [{i},{j}]");
                    continue;
                }

                if (!File.Exists(path))
                {
                    _images[i, j] = null;
                    Interlocked.Increment(ref failedCount);
                    LoggerHelper.Warning($"Image file not found [{i},{j}]: {path}");
                    continue;
                }

                try
                {
                    using var stream = File.OpenRead(path);
                    _images[i, j] = SKBitmap.Decode(stream);

                    if (_images[i, j] != null)
                    {
                        Interlocked.Increment(ref successCount);
                        LoggerHelper.Debug(
                            $"Successfully loaded image [{i},{j}]: {path} ({_images[i, j].Width}x{_images[i, j].Height})");
                    }
                    else
                    {
                        Interlocked.Increment(ref failedCount);
                        LoggerHelper.Error($"Image decoding failed [{i},{j}]: {path}");
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failedCount);
                    LoggerHelper.Error($"Image loading exception [{i},{j}]: {path}", ex);
                    _images[i, j] = null;
                }
            }
        });

        stopwatch.Stop();
        LoggerHelper.Info(
            $"Image loading completed - Success: {successCount}, Failed: {failedCount}, Duration: {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    ///     Process single layer data, generate cropped/fog images for all skins
    /// </summary>
    public List<LayerData> GetLayerData(KeyframeLayer layer, PoolData? poolData, int copyIndex)
    {
        ArgumentNullException.ThrowIfNull(layer);

        LoggerHelper.Debug(
            $"Starting layer data processing - TexId: {layer.TexId}, Guid: {layer.Guid}, CopyIndex: {copyIndex}");

        var results = new ConcurrentBag<LayerData>();

        // Parallel process each skin
        Parallel.For(0, _skinsCount, skinIndex =>
        {
            LayerData? data = null;
            if (layer.TexId >= _images.Length)
            {
                LoggerHelper.Error($"Invalid TexId: {layer.TexId}");
                ToastHelper.Error("Invalid TexId.The image has error");
                return;
            }
            if (layer.Srcquad != null &&
                _images[layer.TexId, skinIndex] != null)
            {
                var rect = ProcessUtility.CalculateRectangle(layer);
                LoggerHelper.Debug($"Processing texture image - SkinIndex: {skinIndex}, Rect: {rect}");

                data = ProcessTextureImage(
                    _images[layer.TexId, skinIndex],
                    rect,
                    layer,
                    poolData,
                    skinIndex,
                    copyIndex
                );
            }
            else if (layer.Fog is { Length: > 0 })
            {
                LoggerHelper.Debug($"Processing fog image - SkinIndex: {skinIndex}, FogCount: {layer.Fog.Length}");
                data = ProcessFogImage(layer, poolData, skinIndex, copyIndex);
            }

            if (data is null)
            {
                LoggerHelper.Debug($"Skipping layer data - SkinIndex: {skinIndex}");
                return;
            }
            // Safely store in nested ConcurrentDictionary
            _layersDataDict
                .GetOrAdd(layer.TexId,
                    _ => new ConcurrentDictionary<int, ConcurrentDictionary<string, ConcurrentBag<LayerData>>>())
                .GetOrAdd(skinIndex, _ => new ConcurrentDictionary<string, ConcurrentBag<LayerData>>())
                .GetOrAdd(layer.Guid, _ => [])
                .Add(data);

            results.Add(data);
        });

        // Sort by SkinIndex to ensure output order
        var sortedResults = results.OrderBy(d => d.SkinIndex).ToList();

        // Safely update global index (only increment when copyIndex=0)
        if (copyIndex != 0) return sortedResults;
        lock (_indexLock)
        {
            _currentImageIndex++;
            LoggerHelper.Debug($"Updated global image index to: {_currentImageIndex}");
        }

        if (sortedResults.Count != 0) return sortedResults;
        ToastHelper.Error("The images has error.");
        LoggerHelper.Error("The images has error");
        throw new NullReferenceException();
    }

    #region Core Processing Logic

    /// <summary>
    ///     Crop texture image and save asynchronously
    /// </summary>
    private LayerData ProcessTextureImage(
        SKBitmap source,
        SKRectI rect,
        KeyframeLayer layer,
        PoolData? poolData,
        int skinIndex,
        int copyIndex)
    {
        var (imageName, imageIndex) = GenerateImageName(layer, poolData, skinIndex, copyIndex);

        LoggerHelper.Debug(
            $"Starting texture image processing - ImageName: {imageName}, Rect: {rect.Width}x{rect.Height}");

        // Async save (with error handling)
        _ = Task.Run(() => SaveCroppedImage(source, rect, imageName))
            .ContinueWith(t => HandleSaveError(t, imageName), TaskContinuationOptions.OnlyOnFaulted);

        var layerData = CreateLayerData(imageName, layer, skinIndex, imageIndex, copyIndex);
        LoggerHelper.Debug($"Texture image processing completed - {imageName}");

        return layerData;
    }

    /// <summary>
    ///     Generate fog image and save asynchronously
    /// </summary>
    private LayerData ProcessFogImage(
        KeyframeLayer layer,
        PoolData? poolData,
        int skinIndex,
        int copyIndex)
    {
        var (imageName, imageIndex) = GenerateImageName(layer, poolData, skinIndex, copyIndex);

        LoggerHelper.Debug($"Starting fog image processing - ImageName: {imageName}, Colors: {layer.Fog?.Length ?? 0}");

        _ = Task.Run(() => SaveFogImage(imageName, 100, 100, layer.Fog))
            .ContinueWith(t => HandleSaveError(t, imageName), TaskContinuationOptions.OnlyOnFaulted);

        var layerData = CreateLayerData(imageName, layer, skinIndex, imageIndex, copyIndex);
        LoggerHelper.Debug($"Fog image processing completed - {imageName}");

        return layerData;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    ///     Generate unique image filename and calculate index
    /// </summary>
    private (string Name, int Index) GenerateImageName(
        KeyframeLayer layer,
        PoolData? poolData,
        int skinIndex,
        int copyIndex)
    {
        var imageIndex = poolData?.LayersData[skinIndex].ImageIndex ?? _currentImageIndex;
        var texIdStr = layer.TexId == ConverterSettingViewModel.FogTexId ? "Fog" : layer.TexId.ToString();
        var name = $"Slice_{imageIndex}_{texIdStr}_{skinIndex}_{copyIndex}";

        // Update layer internal sorting identifier
        layer.ImageNameOrder = imageIndex * 1000 + layer.TexId * 100 + skinIndex * 10 + copyIndex;

        return (name, imageIndex);
    }

    /// <summary>
    ///     Create LayerData object (eliminate duplicate code)
    /// </summary>
    private static LayerData CreateLayerData(
        string imageName,
        KeyframeLayer layer,
        int skinIndex,
        int imageIndex,
        int copyIndex)
    {
        return new LayerData
        {
            SlotAndImageName = imageName,
            KeyframeLayer = layer,
            SkinIndex = skinIndex,
            ImageIndex = imageIndex,
            TexId = layer.TexId.ToString(),
            CopyIndex = copyIndex,
            BaseSkinAttachmentName = $"Slice_{imageIndex}_{layer.TexId}_0_{copyIndex}"
        };
    }

    /// <summary>
    ///     Save cropped image
    /// </summary>
    private static void SaveCroppedImage(SKBitmap source, SKRectI rect, string imageName)
    {
        LoggerHelper.Debug($"Saving cropped image: {imageName}, size: {rect.Width}x{rect.Height}");

        using var cropped = new SKBitmap(rect.Width, rect.Height);
        using (var canvas = new SKCanvas(cropped))
        {
            canvas.DrawBitmap(source, -rect.Left, -rect.Top);
        }

        SaveSkImage(SKImage.FromBitmap(cropped), imageName);
        LoggerHelper.Debug($"Cropped image saved successfully: {imageName}");
    }

    /// <summary>
    ///     Generate and save fog image (support 4-color gradient)
    /// </summary>
    private static void SaveFogImage(string imageName, int width, int height, string[] colors)
    {
        LoggerHelper.Debug($"Saving fog image: {imageName}, colors: {colors.Length}");

        if (colors == null || colors.Length < 4)
        {
            const string errorMsg = "Fog effect requires at least 4 color values";
            LoggerHelper.Error(errorMsg);
            throw new ArgumentException(errorMsg);
        }

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        var skColors = colors.Select(SKColor.Parse).ToArray();
        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(width / 2f, height / 2f),
            Math.Max(width, height) / 2f,
            skColors,
            null,
            SKShaderTileMode.Clamp
        );

        using var paint = new SKPaint();
        paint.Shader = shader;
        canvas.DrawRect(new SKRect(0, 0, width, height), paint);

        SaveSkImage(surface.Snapshot(), imageName);
        LoggerHelper.Debug($"Fog image saved successfully: {imageName}");
    }

    private static void SaveSkImage(SKImage image, string imageName)
    {
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        if (data == null)
        {
            const string errorMsg = "Image encoding failed";
            LoggerHelper.Error(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        var fullPath = Path.Combine(Instances.ConverterSetting.ImageSavePath, $"{imageName}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!); // Ensure directory exists

        using var stream = File.OpenWrite(fullPath);
        data.SaveTo(stream);

        LoggerHelper.Debug($"Image saved to: {fullPath}");
    }

    /// <summary>
    ///     Unified error handling
    /// </summary>
    private static void HandleSaveError(Task task, string imageName)
    {
        if (task.Exception?.InnerException is not { } ex) return;
        LoggerHelper.Error($"Failed to save image [{imageName}]", ex);
    }

    #endregion
}