using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using QTSAvalonia.Helper;
using QTSAvalonia.ViewModels.Pages;
using QTSCore.Data.Quad;
using SkiaSharp;
using Matrix = QTSCore.Utility.Matrix;

namespace QTSCore.Render;

/// <summary>
///     Encapsulates all Quad preview rendering logic: surface/canvas management,
///     attach traversal, image cropping, fog generation and animation-frame evaluation.
/// </summary>
public class QuadRenderer : IDisposable
{
    #region 构造函数

    public QuadRenderer(
        PlayerSettingViewModel settings,
        ObservableCollection<ColorPicker> colorize,
        ObservableCollection<ToggleButton> attributes,
        Dictionary<string, SKColor> colorizeDict,
        Dictionary<string, ToggleButton> attributesDict)
    {
        _settings = settings;
        _colorize = colorize;
        _attributes = attributes;
        _colorizeDict = colorizeDict;
        _attributesDict = attributesDict;
    }

    #endregion

    #region 常量

    private static readonly ushort[] TriangleIndices = [0, 1, 2, 0, 2, 3];

    private const int FogBitmapSize = 256;
    private const float GradientRadius = FogBitmapSize / 2f;

    #endregion

    #region 字段

    private readonly PlayerSettingViewModel _settings;

    private readonly List<SKBitmap> _sourceImages = [];

    // 这些集合与字典由 PlayerViewModel 持有，渲染时按原逻辑就地读写
    private readonly ObservableCollection<ColorPicker> _colorize;
    private readonly ObservableCollection<ToggleButton> _attributes;
    private readonly Dictionary<string, SKColor> _colorizeDict;
    private readonly Dictionary<string, ToggleButton> _attributesDict;

    private SKSurface? _surface;
    private int _canvasSize;

    #endregion

    #region 属性

    public QuadJsonData? QuadData { get; set; }

    /// <summary>
    ///     当前播放时间（帧），供 <see cref="DrawAnimation" /> 评估时间线使用。
    ///     由 PlayerViewModel 在帧变更时同步设置。
    /// </summary>
    public int CurrentTime { get; set; }

    /// <summary>
    ///     当渲染过程中需要重绘时（例如属性开关被切换）触发，由 PlayerViewModel 接管完整重绘流程。
    /// </summary>
    public Action? RequestRedraw { get; set; }

    private int CanvasSize => _settings.CanvasSize;

    private int ImageScaleFactor => _settings.ImageScaleFactor;

    private float CenterX => CanvasSize / 2f;

    private float CenterY => CanvasSize / 2f;

    private SKSurface Surface
    {
        get
        {
            if (CanvasSize == _canvasSize && _surface != null)
                return _surface;

            _surface?.Dispose();
            _surface = SKSurface.Create(new SKImageInfo(CanvasSize, CanvasSize));
            _canvasSize = CanvasSize;
            return _surface;
        }
    }

    private SKCanvas Canvas => Surface.Canvas;

    #endregion

    #region 资源管理

    public void AddSourceImage(SKBitmap bitmap)
    {
        _sourceImages.Add(bitmap);
    }

    public void ClearCanvas()
    {
        Canvas.Clear();
    }

    public SKImage Snapshot()
    {
        return Surface.Snapshot();
    }

    /// <summary>
    ///     释放已加载的源图像、清空画布并重置 Quad 数据，供 PlayerViewModel.ClearResources 调用。
    ///     颜色/属性字典与 UI 集合由 PlayerViewModel 自行清空。
    /// </summary>
    public void Reset()
    {
        foreach (var image in _sourceImages)
            image?.Dispose();
        _sourceImages.Clear();

        Canvas.Clear();

        QuadData = null;
        CurrentTime = 0;
    }

    public void Dispose()
    {
        foreach (var image in _sourceImages)
            image?.Dispose();
        _sourceImages.Clear();

        _surface?.Dispose();
        _surface = null;

        GC.SuppressFinalize(this);
    }

    #endregion

    #region 绘制入口

    public void DrawAttach(Attach? attach, Matrix matrix, SKColor color)
    {
        DrawAttachInternal(attach, matrix, color);
    }

    /// <summary>
    ///     绘制指定骨骼的全部 Bone.Attach（使用单位矩阵与透明初始色）。
    /// </summary>
    public void DrawSkeletonBones(QuadSkeleton? skeleton)
    {
        if (skeleton?.Bone is null) return;

        foreach (var bone in skeleton.Bone)
            DrawAttachInternal(bone.Attach, Matrix.IdentityMatrixBy4X4, SKColors.Transparent);
    }

    private void DrawAttachInternal(Attach? attach, Matrix matrix, SKColor color)
    {
        if (attach is null) return;

        switch (attach.AttachType)
        {
            case AttachType.KeyframeLayer:
                DrawKeyframeLayer(attach, matrix, color);
                break;
            case AttachType.Keyframe:
                DrawKeyframeByAttach(attach, matrix, color);
                break;
            case AttachType.Slot:
                DrawSlot(attach, matrix, color);
                break;
            case AttachType.HitBox:
                DrawHitBox(attach, matrix);
                break;
            case AttachType.Animation:
                // DrawAnimationAttach(attach, matrix, color);
                var (att, mat, clr) = DrawAnimation(attach, matrix, color);
                DrawAttachInternal(att, mat, clr);
                break;
            case AttachType.Skeleton:
                DrawSkeleton(attach, matrix, color);
                break;
            case AttachType.None:
            case AttachType.Mix:
            case AttachType.List:
                break;
            default:
                LoggerHelper.Warning($"Unhandled attach type: {attach.AttachType}");
                break;
        }
    }

    private void DrawSkeleton(Attach attach, Matrix matrix, SKColor color)
    {
        if (QuadData?.Skeleton is null || attach.Id < 0 || attach.Id >= QuadData.Skeleton.Length)
            return;

        var skeleton = QuadData.Skeleton[attach.Id];
        foreach (var bone in skeleton?.Bone ?? [])
            DrawAttachInternal(bone.Attach, matrix, color);
    }

    private void DrawSlot(Attach attach, Matrix matrix, SKColor color)
    {
        if (QuadData?.Slot is null || attach.Id < 0 || attach.Id >= QuadData.Slot.Length)
            return;

        var slot = QuadData.Slot[attach.Id];
        foreach (var att in slot?.Attaches ?? [])
            DrawAttachInternal(att, matrix, color);
    }

    private void DrawKeyframeByAttach(Attach attach, Matrix matrix, SKColor color)
    {
        if (QuadData?.Keyframe is null || attach.Id < 0 || attach.Id >= QuadData.Keyframe.Length)
            return;

        var keyframe = QuadData.Keyframe[attach.Id];
        if (keyframe?.Layers is null) return;

        foreach (var order in keyframe.Order)
        {
            if (order < 0 || order >= keyframe.Layers.Length) continue;

            var layer = keyframe.Layers[order];
            if (layer != null) DrawAttachInternal(layer, matrix, color);
            //DrawKeyframeLayer(layer, matrix, color);
        }
    }

    private void DrawKeyframeLayer(Attach? attach, Matrix matrix, SKColor color)
    {
        if (attach is not KeyframeLayer layer) return;

        // 处理染色
        if (_colorizeDict.TryGetValue(layer.Colorize, out var colorizeColor))
        {
            color = colorizeColor;
        }
        else if (!string.IsNullOrEmpty(layer.Colorize))
        {
            _colorizeDict.TryAdd(layer.Colorize, SKColors.White);
            _colorize.Add(new ColorPicker
            {
                Content = layer.Colorize
            });
        }

        // 检查属性过滤
        if (!CheckLayerAttributes(layer))
            return;

        // Srcquad 是 UV 坐标，需要转换为像素坐标
        if (layer.Srcquad == null || layer.Srcquad.Length < 8 || layer.TexId < 0 || layer.TexId >= _sourceImages.Count)
            return;

        var sourceImage = _sourceImages[layer.TexId];
        var imgWidth = sourceImage.Width;
        var imgHeight = sourceImage.Height;

        // UV 坐标转换为像素坐标
        var x0 = layer.Srcquad[0] * imgWidth;
        var y0 = layer.Srcquad[1] * imgHeight;
        var x1 = layer.Srcquad[2] * imgWidth;
        var y1 = layer.Srcquad[3] * imgHeight;
        var x2 = layer.Srcquad[4] * imgWidth;
        var y2 = layer.Srcquad[5] * imgHeight;
        var x3 = layer.Srcquad[6] * imgWidth;
        var y3 = layer.Srcquad[7] * imgHeight;

        // 计算裁剪包围盒
        var minX = Math.Min(Math.Min(x0, x1), Math.Min(x2, x3));
        var minY = Math.Min(Math.Min(y0, y1), Math.Min(y2, y3));
        var maxX = Math.Max(Math.Max(x0, x1), Math.Max(x2, x3));
        var maxY = Math.Max(Math.Max(y0, y1), Math.Max(y2, y3));

        var srcRect = SKRectI.Create((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
        var skBitmap = CropImage(sourceImage, srcRect);
        if (skBitmap is null) return;

        // 计算裁剪后图片的纹理坐标（相对于裁剪区域的像素坐标）
        var cropWidth = maxX - minX;
        var cropHeight = maxY - minY;
        var texturePoints = new[]
        {
            new SKPoint(x0 - minX, y0 - minY),
            new SKPoint(x1 - minX, y1 - minY),
            new SKPoint(x2 - minX, y2 - minY),
            new SKPoint(x3 - minX, y3 - minY)
        };

        DrawImageWithMatrix(skBitmap, layer, matrix, color, texturePoints);
    }

    private bool CheckLayerAttributes(KeyframeLayer layer)
    {
        foreach (var attr in layer.Attribute)
        {
            if (_attributesDict.ContainsKey(attr)) continue;

            var toggle = new ToggleButton
            {
                IsChecked = true, Content = attr
            };
            toggle.IsCheckedChanged += (_, _) => RequestRedraw?.Invoke();
            _attributes.Add(toggle);
            _attributesDict.Add(attr, toggle);
        }

        foreach (var attr in layer.Attribute)
            if (_attributesDict.TryGetValue(attr, out var toggleSwitch) && toggleSwitch.IsChecked == false)
                return false;

        return true;
    }

    private void DrawImageWithMatrix(SKBitmap skBitmap, KeyframeLayer layer, Matrix matrix, SKColor color,
        SKPoint[] texturePoints)
    {
        var vertexMatrix = matrix * layer.DstMatrix;
        var vertices = vertexMatrix.ToFloatArray();

        var destPoints = new[]
        {
            new SKPoint(vertices[0] * ImageScaleFactor + CenterX, vertices[1] * ImageScaleFactor + CenterY),
            new SKPoint(vertices[2] * ImageScaleFactor + CenterX, vertices[3] * ImageScaleFactor + CenterY),
            new SKPoint(vertices[4] * ImageScaleFactor + CenterX, vertices[5] * ImageScaleFactor + CenterY),
            new SKPoint(vertices[6] * ImageScaleFactor + CenterX, vertices[7] * ImageScaleFactor + CenterY)
        };

        using var verticesObj = SKVertices.CreateCopy(
            SKVertexMode.Triangles,
            destPoints,
            texturePoints,
            null,
            TriangleIndices);
        var colorFilter = CreateColorFilter(color);
        using var shader = SKShader.CreateBitmap(skBitmap);
        using var paint = new SKPaint();
        paint.Shader = shader;
        paint.ColorFilter = colorFilter;
        paint.IsAntialias = true;
        // TODO : add more blend modes
        paint.BlendMode = layer.BlendId > 0 ? SKBlendMode.Plus : SKBlendMode.SrcOver;

        Canvas.DrawVertices(verticesObj, SKBlendMode.SrcOver, paint);
        skBitmap.Dispose();
    }

    private void DrawHitBox(Attach attach, Matrix matrix)
    {
        if (QuadData?.Hitbox is null || attach.Id < 0 || attach.Id >= QuadData.Hitbox.Length)
            return;

        var hitboxes = QuadData.Hitbox[attach.Id]?.Layer;
        if (hitboxes is null) return;

        foreach (var hitbox in hitboxes) DrawHitBoxShape(hitbox, matrix);
    }

    private void DrawHitBoxShape(dynamic hitbox, Matrix matrix)
    {
        var vertices = (matrix * new Matrix(4, 4, hitbox.Hitquad)).ToFloatArray();
        var destPoints = new[]
        {
            new SKPoint(vertices[0] * ImageScaleFactor + CenterX, vertices[1] * ImageScaleFactor + CenterY),
            new SKPoint(vertices[2] * ImageScaleFactor + CenterX, vertices[3] * ImageScaleFactor + CenterY),
            new SKPoint(vertices[4] * ImageScaleFactor + CenterX, vertices[5] * ImageScaleFactor + CenterY),
            new SKPoint(vertices[6] * ImageScaleFactor + CenterX, vertices[7] * ImageScaleFactor + CenterY)
        };

        using var path = new SKPath();
        path.AddPoly(destPoints);

        using var paint = new SKPaint();
        paint.Style = SKPaintStyle.Stroke;
        paint.Color = SKColors.DarkOrange;
        paint.StrokeWidth = 2;
        paint.IsAntialias = true;

        Canvas.DrawPath(path, paint);
    }

    private (Attach attach, Matrix matrix, SKColor color) DrawAnimation(Attach attach, Matrix matrix, SKColor color)
    {
        var result = (new Attach(AttachType.None, -1), matrix, color);
        var animation = QuadData.Animation[attach.Id];
        var (currentFrameIndex, currentTime) = GetAnimationTimeIndex(CurrentTime, animation);
        if (currentFrameIndex < 0) return result;
        var curTimeline = animation.Timeline[currentFrameIndex];
        var nextFrameIndex = currentFrameIndex + 1;
        if (nextFrameIndex >= animation.Timeline.Length)
            nextFrameIndex = !animation.IsLoop ? currentFrameIndex : animation.LoopId;

        var nextTimeline = animation.Timeline[nextFrameIndex];
        result.Item1 = curTimeline.Attach ?? new Attach { AttachType = AttachType.None, Id = -1 };
        if (currentFrameIndex == nextFrameIndex)
            //result.matrix *= curTimeline.AnimationMatrix;
            //TODO: Color multi
            return result;

        var rate = (float)currentTime / curTimeline.Time;
        var m4 = curTimeline.MatrixMixId != -1
            ? curTimeline.AnimationMatrix
            : Matrix.Lerp(curTimeline.AnimationMatrix, nextTimeline.AnimationMatrix, rate);

        return result;
    }

    private (int currentFrameIndex, int currentTime) GetAnimationTimeIndex(int currentTime, Animation animation)
    {
        for (var index = 0; index < animation.Timeline.Length; index++)
        {
            currentTime -= animation.Timeline[index].Time;
            if (currentTime < 0) return (index, -currentTime);
        }

        return animation.IsLoop ? (-1, 0) : (animation.LoopId, currentTime);
    }

    #endregion

    #region 图像处理

    private SKBitmap? GetImage(KeyframeLayer layer, SKRectI srcRect)
    {
        if (layer.TexId >= _sourceImages.Count || layer.TexId < 0)
            return GetFogBitmap(layer.Fog);

        var sourceImage = _sourceImages[layer.TexId];

        // Srcquad 是 UV 坐标（0-1），需要乘以图片尺寸得到像素坐标
        // srcRect 参数是错误的（直接使用了 UV 值），需要重新计算
        if (layer.Srcquad != null && layer.Srcquad.Length >= 8)
        {
            var imgWidth = sourceImage.Width;
            var imgHeight = sourceImage.Height;

            // UV 坐标转换为像素坐标
            var x0 = layer.Srcquad[0] * imgWidth;
            var y0 = layer.Srcquad[1] * imgHeight;
            var x1 = layer.Srcquad[2] * imgWidth;
            var y1 = layer.Srcquad[3] * imgHeight;
            var x2 = layer.Srcquad[4] * imgWidth;
            var y2 = layer.Srcquad[5] * imgHeight;
            var x3 = layer.Srcquad[6] * imgWidth;
            var y3 = layer.Srcquad[7] * imgHeight;

            // 计算包围盒
            var minX = Math.Min(Math.Min(x0, x1), Math.Min(x2, x3));
            var minY = Math.Min(Math.Min(y0, y1), Math.Min(y2, y3));
            var maxX = Math.Max(Math.Max(x0, x1), Math.Max(x2, x3));
            var maxY = Math.Max(Math.Max(y0, y1), Math.Max(y2, y3));

            srcRect = SKRectI.Create((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
        }

        var croppedImage = CropImage(sourceImage, srcRect);

        if (croppedImage is null)
            LoggerHelper.Error("Failed to crop image",
                new InvalidOperationException($"Failed to crop image: {layer.TexId}, srcRect={srcRect}"));

        return croppedImage;
    }

    private static SKBitmap? GetFogBitmap(string[] colors)
    {
        if (colors.Length < 4)
        {
            LoggerHelper.Error("Fog effect requires at least 4 color values");
            return null;
        }

        try
        {
            var skColors = colors.Select(SKColor.Parse).ToArray();
            using var shader = SKShader.CreateRadialGradient(
                new SKPoint(FogBitmapSize / 2f, FogBitmapSize / 2f),
                GradientRadius,
                skColors,
                null,
                SKShaderTileMode.Clamp
            );

            using var surface = SKSurface.Create(new SKImageInfo(FogBitmapSize, FogBitmapSize));
            using var canvas = surface.Canvas;
            using var paint = new SKPaint();
            paint.Shader = shader;

            canvas.DrawRect(new SKRect(0, 0, FogBitmapSize, FogBitmapSize), paint);
            return SKBitmap.FromImage(surface.Snapshot());
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("Failed to create fog bitmap", ex);
            return null;
        }
    }

    private SKBitmap? CropImage(SKBitmap? source, SKRectI srcRect)
    {
        if (source == null || srcRect.Width <= 0 || srcRect.Height <= 0)
            return null;

        var safeRect = SKRectI.Intersect(srcRect, new SKRectI(0, 0, source.Width, source.Height));
        if (safeRect.Width <= 0 || safeRect.Height <= 0)
            return null;

        try
        {
            // 创建目标 Bitmap
            var dstBitmap = new SKBitmap(safeRect.Width, safeRect.Height, source.ColorType, source.AlphaType);
            source.ExtractSubset(dstBitmap, safeRect);
            return dstBitmap;
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("Failed to crop image", ex);
            return null;
        }
    }

    private static SKColorFilter? CreateColorFilter(SKColor color)
    {
        var r = color.Red / 255f;
        var g = color.Green / 255f;
        var b = color.Blue / 255f;

        float[] colorMatrix =
        [
            r, 0, 0, 0, 0,
            0, g, 0, 0, 0,
            0, 0, b, 0, 0,
            0, 0, 0, 1, 0
        ];

        return SKColorFilter.CreateColorMatrix(colorMatrix);
    }

    #endregion
}