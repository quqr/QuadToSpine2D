using System.Collections.Concurrent;
using SkiaSharp;
using QTSCore.Data;
using QTSCore.Data.Quad;
using QTSCore.Utility;

namespace QTSCore.Process
{
    /// <summary>
    /// 图像处理服务：裁剪纹理图集、生成雾效图像
    /// 线程安全设计，支持并行处理多皮肤资源
    /// </summary>
    public class ProcessImages
    {
        private readonly SKBitmap?[,] _images;
        private readonly int _skinsCount;
        private int _currentImageIndex;
        
        // 线程安全的嵌套字典：TexId -> SkinIndex -> Guid -> LayerData列表
        private readonly ConcurrentDictionary<int, 
            ConcurrentDictionary<int, 
                ConcurrentDictionary<string, ConcurrentBag<LayerData>>>> _layersDataDict 
            = new();

        // 全局锁：仅用于保护_currentImageIndex更新（轻量级）
        private readonly Lock _indexLock = new();

        /// <summary>
        /// 初始化图像资源库
        /// </summary>
        public ProcessImages(List<List<string?>> imagesSrc)
        {
            if (imagesSrc == null || imagesSrc.Count == 0 || imagesSrc[0].Count == 0)
                throw new ArgumentException("图像源列表不能为空");

            _skinsCount = imagesSrc[0].Count;
            _images = new SKBitmap[imagesSrc.Count, _skinsCount];
            LoadImagesParallel(imagesSrc);
        }

        /// <summary>
        /// 并行加载所有纹理图像（线程安全）
        /// </summary>
        private void LoadImagesParallel(List<List<string?>> imagesSrc)
        {
            Parallel.For(0, imagesSrc.Count, i =>
            {
                for (var j = 0; j < _skinsCount; j++)
                {
                    var path = imagesSrc[i][j];
                    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    {
                        _images[i, j] = null;
                        continue;
                    }

                    try
                    {
                        using var stream = File.OpenRead(path);
                        _images[i, j] = SKBitmap.Decode(stream);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"加载图像失败 [{i},{j}]: {path} - {ex.Message}");
                        _images[i, j] = null;
                    }
                }
            });
        }

        /// <summary>
        /// 处理单个图层数据，生成所有皮肤的裁剪/雾效图像
        /// </summary>
        public List<LayerData> GetLayerData(KeyframeLayer layer, PoolData? poolData, int copyIndex)
        {
            ArgumentNullException.ThrowIfNull(layer);

            var results = new ConcurrentBag<LayerData>();

            // 并行处理每个皮肤
            Parallel.For(0, _skinsCount, skinIndex =>
            {
                LayerData? data = null;
                
                if (layer.Srcquad != null && 
                    _images[layer.TexId, skinIndex] != null)
                {
                    var rect = ProcessUtility.CalculateRectangle(layer);
                    data = ProcessTextureImage(
                        _images[layer.TexId, skinIndex], 
                        rect, 
                        layer, 
                        poolData, 
                        skinIndex, 
                        copyIndex
                    );
                }
                else if (layer.Fog is { Count: > 0 })
                {
                    data = ProcessFogImage(layer, poolData, skinIndex, copyIndex);
                }

                if (data != null)
                {
                    // 安全存储到嵌套ConcurrentDictionary
                    _layersDataDict
                        .GetOrAdd(layer.TexId, _ => new())
                        .GetOrAdd(skinIndex, _ => new())
                        .GetOrAdd(layer.Guid, _ => new())
                        .Add(data);
                    
                    results.Add(data);
                }
            });

            // 按SkinIndex排序保证输出顺序
            var sortedResults = results.OrderBy(d => d.SkinIndex).ToList();
            
            // 安全更新全局索引（仅当copyIndex=0时递增）
            if (copyIndex == 0)
            {
                lock (_indexLock)
                {
                    _currentImageIndex++;
                }
            }

            return sortedResults;
        }

        #region 核心处理逻辑

        /// <summary>
        /// 裁剪纹理图像并异步保存
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
            
            // 异步保存（带错误处理）
            _ = Task.Run(() => SaveCroppedImage(source, rect, imageName))
                .ContinueWith(t => HandleSaveError(t, imageName), TaskContinuationOptions.OnlyOnFaulted);

            return CreateLayerData(imageName, layer, skinIndex, imageIndex, copyIndex);
        }

        /// <summary>
        /// 生成雾效图像并异步保存
        /// </summary>
        private LayerData ProcessFogImage(
            KeyframeLayer layer, 
            PoolData? poolData, 
            int skinIndex, 
            int copyIndex)
        {
            var (imageName, imageIndex) = GenerateImageName(layer, poolData, skinIndex, copyIndex);
            
            _ = Task.Run(() => SaveFogImage(imageName, 100, 100, layer.Fog!))
                .ContinueWith(t => HandleSaveError(t, imageName), TaskContinuationOptions.OnlyOnFaulted);

            return CreateLayerData(imageName, layer, skinIndex, imageIndex, copyIndex);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成唯一图像文件名并计算索引
        /// </summary>
        private (string Name, int Index) GenerateImageName(
            KeyframeLayer layer, 
            PoolData? poolData, 
            int skinIndex, 
            int copyIndex)
        {
            var imageIndex = poolData?.LayersData[skinIndex].ImageIndex ?? _currentImageIndex;
            var texIdStr = layer.TexId == GlobalData.FogTexId ? "Fog" : layer.TexId.ToString();
            var name = $"Slice_{imageIndex}_{texIdStr}_{skinIndex}_{copyIndex}";
            
            // 更新图层内部排序标识
            layer.ImageNameOrder = imageIndex * 1000 + layer.TexId * 100 + skinIndex * 10 + copyIndex;
            
            return (name, imageIndex);
        }

        /// <summary>
        /// 创建LayerData对象（消除重复代码）
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
        /// 保存裁剪后的图像
        /// </summary>
        private static void SaveCroppedImage(SKBitmap source, SKRectI rect, string imageName)
        {
            using var cropped = new SKBitmap(rect.Width, rect.Height);
            using (var canvas = new SKCanvas(cropped))
            {
                canvas.DrawBitmap(source, -rect.Left, -rect.Top);
            }

            SaveSkImage(SKImage.FromBitmap(cropped), imageName);
        }

        /// <summary>
        /// 生成并保存雾效图像（支持4色渐变）
        /// </summary>
        private static void SaveFogImage(string imageName, int width, int height, List<string> colors)
        {
            if (colors == null || colors.Count < 4)
                throw new ArgumentException("雾效需要至少4个颜色值");

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
        }

        /// <summary>
        /// 通用图像保存逻辑
        /// </summary>
        private static void SaveSkImage(SKImage image, string imageName)
        {
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            if (data == null) throw new InvalidOperationException("图像编码失败");
            
            var fullPath = Path.Combine(GlobalData.ImageSavePath, $"{imageName}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!); // 确保目录存在
            
            using var stream = File.OpenWrite(fullPath);
            data.SaveTo(stream);
        }

        /// <summary>
        /// 统一错误处理
        /// </summary>
        private static void HandleSaveError(Task task, string imageName)
        {

            if (task.Exception?.InnerException is not { } ex) return;
            Console.WriteLine($"保存图像失败 [{imageName}]: {ex.Message}");
            GlobalData.IsCompleted = false;

        }

        #endregion
    }
}