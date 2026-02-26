using System.Collections;
using System.Collections.Specialized;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Skia;
using Microsoft.Extensions.DependencyInjection;
using QTSCore.Data.Quad;
using QTSCore.Process;
using QTSCore.Utility;
using SkiaSharp;

namespace QTSAvalonia.ViewModels.Pages;

[SingletonService]
public partial class PlayerViewModel : ViewModelBase, IDisposable
{
#region 静态常量

    private static readonly PlayerSettingViewModel Settings =
        Instances.ServiceProvider.GetRequiredService<PlayerSettingViewModel>();

    private static readonly ushort[] TriangleIndices = [0, 1, 2, 0, 2, 3];

    private const int FogBitmapSize = 256;
    private const float GradientRadius = FogBitmapSize / 2f;

#endregion

#region 字段

    private readonly List<SKBitmap> _sourceImages = [];
    private readonly Dictionary<string, SKColor> _colorizeDict = [];
    private readonly Dictionary<string, ToggleButton> _attributesDict = [];

    private CancellationTokenSource? _playbackCancellationTokenSource;
    private string _quadFilePath = string.Empty;
    private QuadJsonData? _quadJsonData;
    private int _canvasSize;

    private SKSurface? _surface;

#endregion

#region Observable Properties

    [ObservableProperty] private ObservableCollection<Button> _animations = [];
    [ObservableProperty] private int _currentFrame;
    [ObservableProperty] private IImage? _image;
    [ObservableProperty] private ObservableCollection<string> _imagePaths = [];
    [ObservableProperty] private ObservableCollection<ColorPicker> _colorize = [];
    [ObservableProperty] private ObservableCollection<ToggleButton> _attributes = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private ObservableCollection<Button> _keyframes = [];
    [ObservableProperty] private ObservableCollection<Button> _layers = [];
    [ObservableProperty] private string _quadFileName = string.Empty;
    [ObservableProperty] private ObservableCollection<Button> _skeletons = [];
    [ObservableProperty] private int _time;
    [ObservableProperty] private int _totalFrames;
    [ObservableProperty] private bool _isLoopAnimation;

#endregion

#region 计算属性

    private Animation?    CurrentAnimation { get; set; }
    private QuadSkeleton? CurrentSkeleton  { get; set; }

    private static int   ImageScaleFactor => Settings.ImageScaleFactor;
    private static int   CanvasSize       => Settings.CanvasSize;
    private static float CenterX          => CanvasSize / 2f;
    private static float CenterY          => CanvasSize / 2f;
    private static float Fps              => 1          / Settings.Fps;

    private SKSurface Surface
    {
        get
        {
            if (CanvasSize == _canvasSize && _surface != null)
                return _surface;

            _surface?.Dispose();
            _surface    = SKSurface.Create(new SKImageInfo(CanvasSize, CanvasSize));
            _canvasSize = CanvasSize;
            return _surface;
        }
    }

    private SKCanvas Canvas => Surface.Canvas;

#endregion

#region 构造函数

    public PlayerViewModel()
    {
        Colorize.CollectionChanged += OnColorizeCollectionChanged;
    }

#endregion

#region 事件处理

    private void OnColorizeCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                RegisterColorPickers(e.NewItems);
                break;
            case NotifyCollectionChangedAction.Remove:
                UnregisterColorPickers(e.OldItems);
                break;
        }
    }

    private void RegisterColorPickers(IList? items)
    {
        if (items == null) return;

        foreach (var item in items.OfType<ColorPicker>())
            item.ColorChanged += OnColorPickerColorChanged;
    }

    private void UnregisterColorPickers(IList? items)
    {
        if (items == null) return;

        foreach (var item in items.OfType<ColorPicker>())
        {
            item.ColorChanged -= OnColorPickerColorChanged;
            _colorizeDict.Remove(item.Content?.ToString() ?? string.Empty);
        }
    }

    private void OnColorPickerColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (sender is not ColorPicker colorPicker) return;

        var colorKey = colorPicker.Content?.ToString() ?? string.Empty;
        _colorizeDict[colorKey] = e.NewColor.ToSKColor();
        ReDraw();
    }

#endregion

#region 加载方法

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (string.IsNullOrEmpty(_quadFilePath) || !ImagePaths.Any())
        {
            LoggerHelper.Warning("Missing quad file or images");
            ToastHelper.Error("ERROR", "Please select Quad file and image files first");
            return;
        }

        IsLoading = true;
        try
        {
            Clear();

            var quadTask   = Task.Run(() => LoadQuadFile(_quadFilePath));
            var imagesTask = LoadSourceImagesAsync();

            LoggerHelper.Info("Loading preview data");
            ToastHelper.Info("Loading", "Loading data");

            await Task.WhenAll(quadTask, imagesTask);

            await DispatcherHelper.RunOnMainThreadAsync(() =>
            {
                SetSkeletons();
                LoggerHelper.Info("Preview data loading completed");
                ToastHelper.Success("SUCCESS", "Preview data loaded successfully");
            });
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("Failed to load preview data", ex);
            ToastHelper.Error("ERROR", $"Load failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadQuadFile(string quadPath)
    {
        if (!File.Exists(quadPath))
        {
            LoggerHelper.Error("Quad file not found", new FileNotFoundException("Quad file not found", quadPath));
            ToastHelper.Error("ERROR", "Quad file not found");
            return;
        }

        try
        {
            _quadJsonData = new ProcessQuadData()
                            .LoadQuadJson(quadPath).QuadData;

            if (_quadJsonData is null)
            {
                throw new InvalidOperationException("Failed to parse Quad file");
            }

            LoggerHelper.Debug(
                $"Quad file loaded. Skeletons: {_quadJsonData.Skeleton?.Length ?? 0}, Animations: {_quadJsonData.Animation?.Length ?? 0}");
        }
        catch (JsonException ex)
        {
            LoggerHelper.Error("Invalid Quad file format", ex);
            ToastHelper.Error("ERROR", "Invalid Quad file format");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("Failed to load quad file", ex);
            ToastHelper.Error("ERROR", $"Failed to load quad file: {ex.Message}");
        }
    }

    private async Task LoadSourceImagesAsync()
    {
        var loadTasks = ImagePaths.Select(LoadSingleImageAsync);
        await Task.WhenAll(loadTasks);
    }

    private async Task LoadSingleImageAsync(string path)
    {
        if (!File.Exists(path))
        {
            LoggerHelper.Error("Image file not found", new FileNotFoundException($"Image file not found: {path}"));
            ToastHelper.Error("ERROR", $"Image file not found: {path}");
            return;
        }

        try
        {
            await using var stream  = File.OpenRead(path);
            var             skImage = SKBitmap.Decode(stream);
            if (skImage is null)
            {
                throw new InvalidOperationException($"Cannot decode image: {path}");
            }
            _sourceImages.Add(skImage);
            LoggerHelper.Debug($"Loaded image: {path}");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("Failed to load image", ex);
            ToastHelper.Error("ERROR", $"Failed to load image: {Path.GetFileName(path)}");
        }
    }

#endregion

#region 绘制方法

    [RelayCommand]
    private void DrawAttach(Attach? attach)
    {
        DrawAttachInternal(attach, Matrix.IdentityMatrixBy4X4, SKColors.Transparent);
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
                var (att,mat,clr) = DrawAnimation(attach, matrix, color);
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
        if (_quadJsonData?.Skeleton is null || attach.Id < 0 || attach.Id >= _quadJsonData.Skeleton.Length)
            return;

        var skeleton = _quadJsonData.Skeleton[attach.Id];
        foreach (var bone in skeleton?.Bone ?? [])
            DrawAttachInternal(bone.Attach, matrix, color);
    }

    private void DrawSlot(Attach attach, Matrix matrix, SKColor color)
    {
        if (_quadJsonData?.Slot is null || attach.Id < 0 || attach.Id >= _quadJsonData.Slot.Length)
            return;

        var slot = _quadJsonData.Slot[attach.Id];
        foreach (var att in slot?.Attaches ?? [])
            DrawAttachInternal(att, matrix, color);
    }

    private void DrawKeyframeByAttach(Attach attach, Matrix matrix, SKColor color)
    {
        if (_quadJsonData?.Keyframe is null || attach.Id < 0 || attach.Id >= _quadJsonData.Keyframe.Length)
            return;

        var keyframe = _quadJsonData.Keyframe[attach.Id];
        if (keyframe?.Layers is null) return;

        foreach (var order in keyframe.Order)
        {
            if (order < 0 || order >= keyframe.Layers.Length) continue;

            var layer = keyframe.Layers[order];
            if (layer != null)
            {
                DrawAttachInternal(layer, matrix, color);
                //DrawKeyframeLayer(layer, matrix, color);
            }
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
            Colorize.Add(new ColorPicker
            {
                Content = layer.Colorize
            });
        }

        // 检查属性过滤
        if (!CheckLayerAttributes(layer))
            return;

        var srcRect = SKRectI.Create((int)layer.SrcX, (int)layer.SrcY, (int)layer.Width, (int)layer.Height);
        var skBitmap = GetImage(layer, srcRect);
        if (skBitmap is null) return;

        DrawImageWithMatrix(skBitmap, layer, matrix,color);
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
            toggle.IsCheckedChanged += (_, _) => ReDraw();
            Attributes.Add(toggle);
            _attributesDict.Add(attr, toggle);
        }

        foreach (var attr in layer.Attribute)
        {
            if (_attributesDict.TryGetValue(attr, out var toggleSwitch) && toggleSwitch.IsChecked == false)
                return false;
        }

        return true;
    }

    private void DrawImageWithMatrix(SKBitmap skBitmap, KeyframeLayer layer, Matrix matrix,SKColor color)
    {
        if(layer.Srcquad is null) return;
        
        var vertexMatrix = matrix * layer.DstMatrix;
        var vertices     = vertexMatrix.ToFloatArray();

        var destPoints = new[]
        {
            new SKPoint(vertices[0] * ImageScaleFactor + CenterX, vertices[1] * ImageScaleFactor + CenterY),
            new SKPoint(vertices[2] * ImageScaleFactor + CenterX, vertices[3] * ImageScaleFactor + CenterY),
            new SKPoint(vertices[4] * ImageScaleFactor + CenterX, vertices[5] * ImageScaleFactor + CenterY),
            new SKPoint(vertices[6] * ImageScaleFactor + CenterX, vertices[7] * ImageScaleFactor + CenterY)
        };
        var texturePoints = new[]
        {
            new SKPoint(layer.Srcquad[0] - layer.SrcX, layer.Srcquad[1] - layer.SrcY),
            new SKPoint(layer.Srcquad[2] - layer.SrcX, layer.Srcquad[3] - layer.SrcY),
            new SKPoint(layer.Srcquad[4] - layer.SrcX, layer.Srcquad[5] - layer.SrcY),
            new SKPoint(layer.Srcquad[6] - layer.SrcX, layer.Srcquad[7] - layer.SrcY)
        };

        using var verticesObj = SKVertices.CreateCopy(
            SKVertexMode.Triangles,
            destPoints,
            texturePoints,
            null,
            TriangleIndices);
        var       colorFilter = CreateColorFilter(color);
        using var shader      = SKShader.CreateBitmap(skBitmap);
        using var paint       = new SKPaint();
        paint.Shader      = shader;
        paint.ColorFilter = colorFilter;
        paint.IsAntialias = true;
        // TODO : add more blend modes
        paint.BlendMode = layer.BlendId > 0 ? SKBlendMode.Plus : SKBlendMode.SrcOver;

        Canvas.DrawVertices(verticesObj, SKBlendMode.SrcOver, paint);
        skBitmap.Dispose();
    }

    private void DrawHitBox(Attach attach, Matrix matrix)
    {
        if (_quadJsonData?.Hitbox is null || attach.Id < 0 || attach.Id >= _quadJsonData.Hitbox.Length)
            return;

        var hitboxes = _quadJsonData.Hitbox[attach.Id]?.Layer;
        if (hitboxes is null) return;

        foreach (var hitbox in hitboxes)
        {
            DrawHitBoxShape(hitbox, matrix);
        }
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
        paint.Style       = SKPaintStyle.Stroke;
        paint.Color       = SKColors.DarkOrange;
        paint.StrokeWidth = 2;
        paint.IsAntialias = true;

        Canvas.DrawPath(path, paint);
    }

    private (Attach attach, Matrix matrix, SKColor color) DrawAnimation(Attach attach, Matrix matrix, SKColor color)
    {
        var result    = (new Attach(AttachType.None, -1), matrix, color);
        var animation = _quadJsonData.Animation[attach.Id];
        var (currentFrameIndex, currentTime) = GetAnimationTimeIndex(Time, animation);
        if (currentFrameIndex < 0) return result;
        var curTimeline    = animation.Timeline[currentFrameIndex];
        var nextFrameIndex = currentFrameIndex + 1;
        if (nextFrameIndex >= animation.Timeline.Length)
            nextFrameIndex = !animation.IsLoop ? currentFrameIndex : animation.LoopId;

        var nextTimeline = animation.Timeline[nextFrameIndex];
        result.Item1 = curTimeline.Attach ?? new Attach { AttachType = AttachType.None, Id = -1 };
        if (currentFrameIndex == nextFrameIndex)
        {
            //result.matrix *= curTimeline.AnimationMatrix;
            //TODO: Color multi
            return result;
        }

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

        var sourceImage  = _sourceImages[layer.TexId];
        var croppedImage = CropImage(sourceImage, srcRect);

        if (croppedImage is null)
        {
            LoggerHelper.Error("Failed to crop image",
                new InvalidOperationException($"Failed to crop image: {layer.TexId}"));
            ToastHelper.Error("ERROR", $"Failed to crop image: {layer.TexId}");
        }

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
            using var canvas  = surface.Canvas;
            using var paint   = new SKPaint();
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
        var r = color.Red   / 255f;
        var g = color.Green / 255f;
        var b = color.Blue  / 255f;

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

#region 播放控制

    [RelayCommand]
    private async Task PlayAnimationAsync()
    {
        if (CurrentAnimation?.Timeline is null || CurrentAnimation.Timeline.Length == 0)
        {
            LoggerHelper.Warning("No valid animation to play");
            ToastHelper.Warn("WARNING", "No animation available to play");
            return;
        }

        if (IsPlaying)
        {
            StopPlayback();
            return;
        }

        IsPlaying = true;
        LoggerHelper.Info($"Playing animation from frame {CurrentFrame}");

        _playbackCancellationTokenSource = new CancellationTokenSource();
        var token = _playbackCancellationTokenSource.Token;

        try
        {
            for (var i = CurrentFrame; i <= TotalFrames && !token.IsCancellationRequested;)
            {
                CurrentFrame = i;
                await Task.Delay(TimeSpan.FromSeconds(Fps), token);
                i++;
                if (i >= TotalFrames && IsLoopAnimation)
                {
                    i = 0;
                }
            }

            LoggerHelper.Info("Animation playback completed");
        }
        catch (OperationCanceledException)
        {
            LoggerHelper.Debug("Animation playback cancelled");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("Animation playback error", ex);
        }
        finally
        {
            IsPlaying = false;
            _playbackCancellationTokenSource?.Dispose();
            _playbackCancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void StopPlayback()
    {
        _playbackCancellationTokenSource?.Cancel();
        IsPlaying = false;
        LoggerHelper.Debug("Playback stopped");
    }

    [RelayCommand]
    private void SetNextFrame()
    {
        if (CurrentAnimation?.Timeline is null) return;
        CurrentFrame = CurrentFrame >= TotalFrames - 1 ? 0 : CurrentFrame + 1;
        LoggerHelper.Debug($"Frame changed to: {CurrentFrame}");
    }

    [RelayCommand]
    private void SetPreviousFrame()
    {
        if (CurrentAnimation?.Timeline is null) return;
        CurrentFrame = CurrentFrame <= 0 ? TotalFrames - 1 : CurrentFrame - 1;
        LoggerHelper.Debug($"Frame changed to: {CurrentFrame}");
    }

    partial void OnCurrentFrameChanged(int value)
    {
        Canvas.Clear();
        Time = value;

        if (CurrentSkeleton?.Bone != null)
        {
            foreach (var bone in CurrentSkeleton.Bone)
                DrawAttachInternal(bone.Attach, Matrix.IdentityMatrixBy4X4, SKColors.Transparent);
        }

        Render();
    }

    private void ReDraw()
    {
        Canvas.Clear();

        if (CurrentSkeleton?.Bone != null)
        {
            foreach (var bone in CurrentSkeleton.Bone)
                DrawAttachInternal(bone.Attach, Matrix.IdentityMatrixBy4X4, SKColors.Transparent);
        }

        Render();
    }

    private void Render()
    {
        Image = Surface.Snapshot().ToAvaloniaImage();
    }

#endregion

#region 动画/骨骼设置

    [RelayCommand]
    private void SetAnimations(QuadSkeleton? skeleton)
    {
        if (skeleton?.Bone is null)
        {
            LoggerHelper.Warning("Invalid skeleton or no bones");
            return;
        }

        if (CurrentSkeleton == skeleton) return;

        Animations.Clear();
        Keyframes.Clear();
        Layers.Clear();
        CurrentSkeleton = skeleton;

        foreach (var bone in skeleton.Bone.Where(b => b.Attach is not null))
        {
            Animations.Add(new Button
            {
                Content = $"{bone.Attach.AttachType} {bone.Attach.Id}", Command = SetKeyframesCommand, CommandParameter = bone.Attach
            });
        }

        LoggerHelper.Debug($"Set {Animations.Count} animations for skeleton: {skeleton.Name}");
    }

    [RelayCommand]
    private void SetKeyframes(Attach? attach)
    {
        if (attach?.AttachType != AttachType.Animation)
        {
            LoggerHelper.Warning($"Invalid attach type: {attach?.AttachType}");
            return;
        }

        var animation = GetSafeAnimation(attach.Id);
        if (animation?.Timeline is null)
        {
            LoggerHelper.Warning($"Animation not found or invalid: ID {attach.Id}");
            return;
        }

        if (CurrentAnimation == animation) return;

        CurrentAnimation = animation;
        CurrentFrame     = 0;
        TotalFrames      = animation.Timeline.Sum(x => x.Time);

        Keyframes.Clear();
        Layers.Clear();
        Attributes.Clear();
        _attributesDict.Clear();
        Colorize.Clear();
        _colorizeDict.Clear();

        foreach (var timeline in animation.Timeline.Where(t => t.Attach is not null))
        {
            switch (timeline.Attach.AttachType)
            {
                case AttachType.Keyframe:
                    Keyframes.Add(new Button
                    {
                        Content = $"{timeline.Attach.AttachType} {timeline.Attach.Id}", Command = SetLayersCommand, CommandParameter = timeline.Attach
                    });
                    break;
                case AttachType.HitBox:
                    Keyframes.Add(new Button
                    {
                        Content = $"{timeline.Attach.AttachType} {timeline.Attach.Id}", Command = DrawHitboxAttachCommand, CommandParameter = timeline.Attach
                    });
                    break;
            }
        }

        LoggerHelper.Debug($"Set {Keyframes.Count} animation timelines");
    }

    [RelayCommand]
    private void SetLayers(Attach? attach)
    {
        if (attach is null) return;

        Layers.Clear();

        if (_quadJsonData?.Keyframe is null || attach.Id < 0 || attach.Id >= _quadJsonData.Keyframe.Length)
            return;

        var layers = _quadJsonData.Keyframe[attach.Id]?.Layers;
        if (layers is null) return;

        for (var index = 0; index < layers.Length; index++)
        {
            Layers.Add(new Button
            {
                Content = $"layer {index}", Command = DrawKeyframeLayerAttachCommand, CommandParameter = layers[index]
            });
        }

        Canvas.Clear();
        DrawAttachInternal(attach, Matrix.IdentityMatrixBy4X4, SKColors.Transparent);
        Render();
    }

    [RelayCommand]
    private void DrawKeyframeLayerAttach(KeyframeLayer? attach)
    {
        Canvas.Clear();
        DrawAttachInternal(attach, Matrix.IdentityMatrixBy4X4, SKColors.Transparent);
        Render();
    }

    [RelayCommand]
    private void DrawHitboxAttach(Attach? attach)
    {
        Canvas.Clear();
        DrawAttachInternal(attach, Matrix.IdentityMatrixBy4X4, SKColors.Transparent);
        Render();
    }

    private void SetSkeletons()
    {
        LoggerHelper.Debug("Setting skeletons");

        if (_quadJsonData?.Skeleton is null)
        {
            LoggerHelper.Warning("QuadJsonData or Skeleton is null");
            return;
        }

        Skeletons.Clear();
        foreach (var skeleton in _quadJsonData.Skeleton.Where(s => s != null))
        {
            Skeletons.Add(new Button
            {
                Content = skeleton.Name, Command = SetAnimationsCommand, CommandParameter = skeleton
            });
        }

        LoggerHelper.Debug($"Set {Skeletons.Count} skeletons");
    }

#endregion

#region 文件选择

    [RelayCommand]
    private async Task OpenQuadFilePickerAsync()
    {
        LoggerHelper.Info("Opening quad file picker");
        var file = await AvaloniaFilePickerService.OpenQuadFileAsync();
        if (file?.Count > 0)
        {
            QuadFileName  = file[0].Name;
            _quadFilePath = file[0].Path.LocalPath;
            LoggerHelper.Info($"Selected quad file: {QuadFileName}");
            ToastHelper.Success("SUCCESS", $"Selected: {QuadFileName}");
        }
    }

    [RelayCommand]
    private async Task OpenImageFilePickerAsync()
    {
        LoggerHelper.Info("Opening image file picker");
        var files = await AvaloniaFilePickerService.OpenImageFilesAsync();
        if (files?.Count > 0)
        {
            ImagePaths.Clear();
            foreach (var file in files)
            {
                var imagePath = file.Path.LocalPath;
                ImagePaths.Add(imagePath);
                LoggerHelper.Debug($"Added image path: {imagePath}");
            }

            LoggerHelper.Info($"Selected {ImagePaths.Count} image files");
            ToastHelper.Success("SUCCESS", $"Selected {ImagePaths.Count} image files");
        }
    }

#endregion

#region 清理和释放资源

    private Animation? GetSafeAnimation(int id)
    {
        if (_quadJsonData?.Animation is null || id < 0 || id >= _quadJsonData.Animation.Length)
            return null;

        return _quadJsonData.Animation[id];
    }

    private void ClearResources()
    {
        CurrentFrame = 0;
        TotalFrames  = 0;
        Image        = null;

        foreach (var image in _sourceImages)
            image?.Dispose();

        Colorize.Clear();
        _colorizeDict.Clear();
        Attributes.Clear();
        _attributesDict.Clear();

        _sourceImages.Clear();

        Canvas.Clear();

        _quadJsonData    = null;
        CurrentAnimation = null;
        CurrentSkeleton  = null;
    }

    [RelayCommand]
    private void Clear()
    {
        LoggerHelper.Debug("Clearing preview data");

        StopPlayback();

        Skeletons.Clear();
        Animations.Clear();
        Keyframes.Clear();
        Layers.Clear();

        ClearResources();

        LoggerHelper.Debug("Preview data cleared");
    }

    public void Dispose()
    {
        StopPlayback();
        ClearResources();
        _surface?.Dispose();
        _surface = null;

        GC.SuppressFinalize(this);
    }

#endregion

#region 快速加载（调试用）

    [RelayCommand]
    private async Task QuicklyLoad()
    {
        // _quadFilePath = "/Users/loop/Downloads/Test/swi unic BlackKnight_HG_M.mbs.v55.quad";
        // ImagePaths =
        // [
        //     "/Users/loop/Downloads/Test/swi unic BlackKnight_HG_M00.0.nvt.png",
        //     "/Users/loop/Downloads/Test/swi unic BlackKnight_HG_M00.1.nvt.png",
        // ];
        _quadFilePath = @"F:\Codes\Test\swi sent Fuyusaka00.mbs.v55.quad";
        ImagePaths =
        [
            @"F:\Codes\Test\swi sent Fuyusaka00.0.nvt.png",
            @"F:\Codes\Test\swi sent Fuyusaka00.1.nvt.png",
        ];

        await LoadAsync();
    }

#endregion
}