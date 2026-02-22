using System.Drawing;
using System.Threading;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using QTSCore.Data.Quad;
using QTSCore.Process;
using QTSCore.Utility;
using SkiaSharp;

namespace QTSAvalonia.ViewModels.Pages;

[SingletonService]
public partial class PlayerViewModel : ViewModelBase, IDisposable
{
    private static readonly PlayerSettingViewModel Settings =
        Instances.ServiceProvider.GetRequiredService<PlayerSettingViewModel>();

    private static readonly ushort[] Indices = [0, 1, 2, 0, 2, 3];
    private readonly Dictionary<string, Bitmap> _frameCache = new(); // Frame cache
    private readonly List<SKBitmap> _sourceImages = [];
    private readonly SKSurface _surface = SKSurface.Create(new SKImageInfo(CanvasSize, CanvasSize));

    [ObservableProperty] private ObservableCollection<Button> _animations = [];

    [ObservableProperty] private int _currentFrame;

    [ObservableProperty] private Bitmap? _image;


    [ObservableProperty] private ObservableCollection<string> _imagePaths = [];

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private bool _isPlaying;

    [ObservableProperty] private ObservableCollection<Button> _keyframes = [];
    [ObservableProperty] private ObservableCollection<Button> _layers = [];
    private CancellationTokenSource? _playbackCancellationTokenSource;

    [ObservableProperty] private string _quadFileName = string.Empty;

    private string _quadFilePath = string.Empty;

    private QuadJsonData? _quadJsonData;

    [ObservableProperty] private ObservableCollection<Button> _skeletons = [];
    [ObservableProperty] private int _time;

    [ObservableProperty] private int _totalFrames;

    private int ImageScaleFactor => Settings.ImageScaleFactor;

    private static int CanvasSize => Settings.CanvasSize;
    private static float CenterX => CanvasSize / 2f;
    private static float CenterY => CanvasSize / 2f;
    private float Fps => 1 / Settings.Fps;

    private Animation? CurrentAnimation { get; set; }
    private QuadSkeleton? CurrentSkeleton { get; set; }
    private SKCanvas Canvas => _surface.Canvas;

    public void Dispose()
    {
        StopPlayback();
        ClearResources();

        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void DrawAttach(Attach? attach)
    {
        DrawAttach(attach, Matrix.IdentityMatrixBy4X4, Color.White);
    }    

    private void DrawAttach(Attach? attach, Matrix matrix, Color color)
    {
        if (attach is null) return;
        switch (attach.AttachType)
        {
            case AttachType.KeyframeLayer:
                DrawKeyframeLayer(attach,matrix,color);
                break;
            case AttachType.Keyframe:
                DrawKeyframe(attach, matrix, color);
                break;
            case AttachType.Slot:
                DrawSlot(attach, matrix, color);
                break;
            case AttachType.HitBox:
                DrawHitBox(attach, matrix, color);
                break;
            case AttachType.Animation:
                var (att, mat, col) = DrawAnimation(attach, matrix, color);
                if (att.AttachType == AttachType.None) return;
                DrawAttach(att, mat, col);
                break;
            case AttachType.Skeleton:
                DrawSkeleton(attach, matrix, color);
                break;
            case AttachType.Mix:
            case AttachType.List:
                break;
            case AttachType.None:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void DrawKeyframeLayer(Attach attach, Matrix matrix, Color color)
    {
        if (attach is not KeyframeLayer keyframeLayer) return;
        DrawKeyframeLayer(matrix,keyframeLayer);
    }

    private void DrawSkeleton(Attach attach, Matrix matrix, Color color)
    {
        foreach (var bone in _quadJsonData.Skeleton[attach.Id].Bone) DrawAttach(bone.Attach, matrix, color);
    }

    private (int currentFrameIndex, int currentTime) GetAnimationTimeIndex(int currentTime, Animation animation)
    {
        for (var index = 0; index < animation.Timeline.Count; index++)
        {
            currentTime -= animation.Timeline[index].Time;
            if (currentTime < 0) return (index, -currentTime);
        }

        return animation.IsLoop ? (-1, 0) : (animation.LoopId, currentTime);
    }

    private (Attach attach, Matrix matrix, Color color) DrawAnimation(Attach attach, Matrix matrix, Color color)
    {
        var result = (new Attach(AttachType.None, -1), matrix, color);
        var animation = _quadJsonData.Animation[attach.Id];
        var (currentFrameIndex, currentTime) = GetAnimationTimeIndex(Time, animation);
        if (currentFrameIndex < 0) return result;
        var curTimeline = animation.Timeline[currentFrameIndex];
        var nextFrameIndex = currentFrameIndex + 1;
        if (nextFrameIndex >= animation.Timeline.Count)
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
        var m4 = curTimeline.IsMatrixMix
            ? curTimeline.AnimationMatrix
            : Matrix.Lerp(curTimeline.AnimationMatrix, nextTimeline.AnimationMatrix, rate);
        //result.matrix *= m4; bugs
        //TODO : Lerp color

        return result;
    }

    private void DrawHitBox(Attach attach, Matrix matrix, Color color)
    {
        var hitboxes = _quadJsonData.Hitbox[attach.Id]?.Layer;
        if (hitboxes is null) return;

        foreach (var hitbox in hitboxes)
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
            paint.Style       = SKPaintStyle.Stroke;      // 描边模式
            paint.Color       = SKColor.Parse("#ffffff"); // 设置颜色
            paint.StrokeWidth = 2;                        // 描边宽度
            paint.IsAntialias = true;                     // 抗锯齿

            // 绘制矩形
            Canvas.DrawPath(path, paint);
        }
    }


    private void DrawSlot(Attach attach, Matrix matrix, Color color)
    {
        foreach (var att in _quadJsonData.Slot[attach.Id].Attaches) DrawAttach(att, matrix, color);
    }

    private void DrawKeyframe(Attach attach, Matrix matrix, Color color)
    {
        var keyframe = _quadJsonData.Keyframe[attach.Id];
        if (keyframe?.Layers is null) return;

        foreach (var order in keyframe.Order)
        {
            var layer = keyframe.Layers[order];
            if (layer is null) continue;
            
            DrawKeyframeLayer(matrix, layer);
        }
    }

    private void DrawKeyframeLayer(Matrix matrix, KeyframeLayer layer)
    {

        var       srcRect      = SKRectI.Create((int)layer.SrcX, (int)layer.SrcY, (int)layer.Width, (int)layer.Height);
        using var sourceBitmap = GetImage(layer, srcRect);
        if (sourceBitmap is null) return;

        // Calculate transformation matrix
        var vertexMatrix = matrix * layer.DstMatrix;
        var vertices     = vertexMatrix.ToFloatArray();

        // Target points (relative to canvas center)
        var destPoints = new[]
        {
            new SKPoint(vertices[0] * ImageScaleFactor + CenterX, vertices[1] * ImageScaleFactor + CenterY),
            new SKPoint(vertices[2] * ImageScaleFactor + CenterX, vertices[3] * ImageScaleFactor + CenterY),
            new SKPoint(vertices[4] * ImageScaleFactor + CenterX, vertices[5] * ImageScaleFactor + CenterY),
            new SKPoint(vertices[6] * ImageScaleFactor + CenterX, vertices[7] * ImageScaleFactor + CenterY)
        };
        foreach (var point in destPoints)
        {
            if (point.X < -CenterX )
            {
                Console.WriteLine();
            }
        }
        // Texture coordinates (relative to crop area)
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
            Indices);

        using var shader = SKShader.CreateBitmap(sourceBitmap);
        using var paint  = new SKPaint();
        paint.Shader      = shader;
        paint.IsAntialias = true;
        paint.BlendMode   = layer.BlendId > 0 ? SKBlendMode.Plus : SKBlendMode.SrcOver;

        Canvas.DrawVertices(
            verticesObj,
            SKBlendMode.SrcOver,
            paint);
    }

    private SKBitmap? GetImage(KeyframeLayer layer, SKRectI srcRect)
    {
        if (layer.TexId >= _sourceImages.Count || layer.TexId < 0) return GetFogBitmap(layer.Fog);
        var sourceBitmap = _sourceImages[layer.TexId];
        var croppedImage = CropBitmap(sourceBitmap, srcRect);
        if (croppedImage is null)
        {
            LoggerHelper.Error("Failed to crop image",
                new InvalidOperationException($"Failed to crop image: {layer.TexId}"));
            ToastHelper.Error("ERROR", $"Failed to crop image: {layer.TexId}");
            return null;
        }

        return croppedImage;
    }

    private static SKBitmap? GetFogBitmap(List<string> colors)
    {
        if (colors.Count < 4)
        {
            LoggerHelper.Error("Fog effect requires at least 4 color values");
            return null;
        }

        var skColors = colors.Select(SKColor.Parse).ToArray();
        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(128f, 128f),
            Math.Max(256, 256) / 2f,
            skColors,
            null,
            SKShaderTileMode.Clamp
        );
        using var surface = SKSurface.Create(new SKImageInfo(256, 256));
        using var canvas = surface.Canvas;
        using var paint = new SKPaint();
        paint.Shader = shader;
        canvas.DrawRect(new SKRect(0, 0, 256, 256), paint);
        return SKBitmap.FromImage(surface.Snapshot());
    }

    private void Render()
    {
        Image = SKBitmap.FromImage(_surface.Snapshot()).ToBitmap();
        //_frameCache.TryAdd(GenerateCacheKey(CurrentAnimation, CurrentFrame), Image);
    }

    private SKBitmap? CropBitmap(SKBitmap? source, SKRectI srcRect)
    {
        if (source == null || srcRect.Width <= 0 || srcRect.Height <= 0)
            return null;

        // Boundary protection
        var safeRect = SKRectI.Intersect(srcRect, new SKRectI(0, 0, source.Width, source.Height));
        if (safeRect.Width <= 0 || safeRect.Height <= 0)
            return null;

        var cropped = new SKBitmap(safeRect.Width, safeRect.Height);
        using var canvas = new SKCanvas(cropped);
        canvas.DrawBitmap(source, -safeRect.Left, -safeRect.Top);

        return cropped;
    }

    [RelayCommand]
    public void Draw(Attach attach)
    {
        DrawAttach(attach, Matrix.IdentityMatrixBy4X4, Color.White);
    }

    private void SetSkeletons()
    {
        LoggerHelper.Debug("Setting skeletons");

        if (_quadJsonData?.Skeleton is null)
        {
            LoggerHelper.Warn("QuadJsonData or Skeleton is null");
            return;
        }

        Skeletons.Clear();
        foreach (var skeleton in _quadJsonData.Skeleton.Where(s => s != null))
            Skeletons.Add(new Button
            {
                Content = skeleton.Name, Command = SetAnimationsCommand, CommandParameter = skeleton
            });

        LoggerHelper.Debug($"Set {Skeletons.Count} skeletons");
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (string.IsNullOrEmpty(_quadFilePath) || !ImagePaths.Any())
        {
            LoggerHelper.Warn("Missing quad file or images");
            ToastHelper.Error("ERROR", "Please select Quad file and image files first");
            return;
        }

        IsLoading = true;
        try
        {
            Clear();
            var quadTask = Task.Run(() => LoadQuadFile(_quadFilePath));
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
        LoggerHelper.Info($"Loading quad file: {quadPath}");

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
                LoggerHelper.Error("Failed to parse Quad file",
                    new InvalidOperationException("Failed to parse Quad file"));
                ToastHelper.Error("ERROR", "Failed to parse Quad file");
                return;
            }

            LoggerHelper.Debug(
                $"Quad file loaded. Skeletons: {_quadJsonData.Skeleton?.Count ?? 0}, Animations: {_quadJsonData.Animation?.Count ?? 0}");
        }
        catch (JsonException ex)
        {
            LoggerHelper.Error("Invalid Quad file format", ex);
            ToastHelper.Error("ERROR", "Invalid Quad file format");
        }
    }

    private async Task LoadSourceImagesAsync()
    {
        var loadTasks = ImagePaths.Select(async path =>
        {
            if (!File.Exists(path))
            {
                LoggerHelper.Error("Image file not found", new FileNotFoundException($"Image file not found: {path}"));
                ToastHelper.Error("ERROR", $"Image file not found: {path}");
                return;
            }

            try
            {
                await using var stream = File.OpenRead(path);
                var skImage = SKBitmap.Decode(stream);
                if (skImage is null)
                {
                    LoggerHelper.Error("Cannot decode image",
                        new InvalidOperationException($"Cannot decode image: {path}"));
                    ToastHelper.Error("ERROR", $"Cannot decode image: {path}");
                    return;
                }

                _sourceImages.Add(skImage);
                LoggerHelper.Debug($"Loaded image: {path}");
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Failed to load image",
                    new InvalidOperationException($"Failed to load image: {path}", ex));
                ToastHelper.Error("ERROR", $"Failed to load image: {path}");
            }
        });

        await Task.WhenAll(loadTasks);
    }

    [RelayCommand]
    private async Task PlayAnimationAsync()
    {
        if (CurrentAnimation?.Timeline is null || CurrentAnimation.Timeline.Count == 0)
        {
            LoggerHelper.Warn("No valid animation to play");
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
            for (var i = CurrentFrame; i < TotalFrames - 1 && !token.IsCancellationRequested; i++)
            {
                CurrentFrame = i;
                await Task.Delay(TimeSpan.FromSeconds(Fps), token);
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

    private string GenerateCacheKey(object obj, int frameIndex)
    {
        return $"{obj.GetHashCode()}_{frameIndex}";
    }

    [RelayCommand]
    private async Task CacheKeyframes()
    {
        ToastHelper.Info("INFO", "Caching keyframes");
        LoggerHelper.Info("Caching keyframes");
        DispatcherHelper.RunOnMainThreadAsync(() =>
        {
            if (CurrentAnimation == null) return;
            for (var i = 0; i < TotalFrames; i++)
            {
                Canvas.Clear();
                Time = i;
                if (_frameCache.TryGetValue(GenerateCacheKey(CurrentAnimation, i), out _)) continue;

                foreach (var bone in CurrentSkeleton.Bone) Draw(bone.Attach);

                _frameCache.TryAdd(GenerateCacheKey(CurrentAnimation, CurrentFrame),
                    SKBitmap.FromImage(_surface.Snapshot()).ToBitmap());
                LoggerHelper.Debug($"Frame cached: {GenerateCacheKey(CurrentAnimation, i)}");
            }

            ToastHelper.Success("SUCCESS", "All keyframes cached");
            LoggerHelper.Info("All keyframes cached");
        });
    }

    partial void OnCurrentFrameChanged(int value)
    {
        Canvas.Clear();
        Time = value;
        if (_frameCache.TryGetValue(GenerateCacheKey(CurrentAnimation, value), out var cachedBitmap))
        {
            Image = cachedBitmap;
            return;
        }

        foreach (var bone in CurrentSkeleton.Bone) Draw(bone.Attach);

        Render();
    }

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


    private Animation? GetSafeAnimation(int id)
    {
        if (_quadJsonData?.Animation is null || id < 0 || id >= _quadJsonData.Animation.Count)
            return null;
        return _quadJsonData.Animation[id];
    }

    [RelayCommand]
    private void SetKeyframes(Attach? attach)
    {
        if (attach?.AttachType != AttachType.Animation)
        {
            LoggerHelper.Warn($"Invalid attach type: {attach?.AttachType}");
            return;
        }

        var animation = GetSafeAnimation(attach.Id);
        if (animation?.Timeline is null)
        {
            LoggerHelper.Warn($"Animation not found or invalid: ID {attach.Id}");
            return;
        }

        if (CurrentAnimation == animation) return;

        CurrentAnimation = animation;
        CurrentFrame = 0;
        TotalFrames = animation.Timeline.Sum(x => x.Time);
        Keyframes.Clear();

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
                        Content          = $"{timeline.Attach.AttachType} {timeline.Attach.Id}", Command = DrawHitboxAttachCommand,
                        CommandParameter = timeline.Attach
                    });
                    break;
            }

        }

        LoggerHelper.Debug($"Set {Keyframes.Count} animation timelines");
    }
    
    [RelayCommand]
    private void SetLayers(Attach? attach)
    {
        if(attach is null) return;
        Layers.Clear();
        var layers = _quadJsonData.Keyframe[attach.Id].Layers;
        if (layers is null) return;
        for (var index = 0; index < layers.Count; index++)
        {
            Layers.Add(new Button
            {
                Content = $"layer {index}", Command = DrawKeyframeLayerAttachCommand, CommandParameter = layers[index]
            });
        }
        Canvas.Clear();
        DrawAttach(attach);
        Render();
    }
    [RelayCommand]
    private void DrawKeyframeLayerAttach(KeyframeLayer? attach)
    {
        Canvas.Clear();
        DrawAttach(attach);
        Render();
    }    
    [RelayCommand]
    private void DrawHitboxAttach(Attach? attach)
    {
        Canvas.Clear();
        DrawAttach(attach);
        Render();
    }
    [RelayCommand]
    private void SetAnimations(QuadSkeleton? skeleton)
    {
        if (skeleton?.Bone is null)
        {
            LoggerHelper.Warn("Invalid skeleton or no bones");
            return;
        }

        if (CurrentSkeleton == skeleton) return;

        Animations.Clear();
        Keyframes.Clear();
        Layers.Clear();
        CurrentSkeleton = skeleton;
        foreach (var bone in skeleton.Bone.Where(b => b.Attach is not null))
            Animations.Add(new Button
            {
                Content = $"{bone.Attach.AttachType} {bone.Attach.Id}", Command = SetKeyframesCommand,
                CommandParameter = bone.Attach
            });

        LoggerHelper.Debug($"Set {Animations.Count} animations for skeleton: {skeleton.Name}");
    }

    private void ClearResources()
    {
        CurrentFrame = 0;
        TotalFrames = 0;
        // Clean up image resources
        Image?.Dispose();
        Image = null;
        foreach (var bitmap in _sourceImages) bitmap?.Dispose();

        _sourceImages.Clear();

        // Clean up cache
        foreach (var (_, cachedBitmap) in _frameCache) cachedBitmap?.Dispose();

        _frameCache.Clear();
        Canvas.Clear();
        // Clean up data
        _quadJsonData = null;
        CurrentAnimation = null;
        CurrentSkeleton = null;
    }

    [RelayCommand]
    private void Clear()
    {
        LoggerHelper.Debug("Clearing preview data");

        StopPlayback();

        // Clear UI collections
        Skeletons.Clear();
        Animations.Clear();
        Keyframes.Clear();
        Layers.Clear();
        // Clear resources
        ClearResources();

        LoggerHelper.Debug("Preview data cleared");
    }

    [RelayCommand]
    private async Task QuicklyLoad()
    {
        // _quadFilePath = "/Users/loop/Downloads/Test/ps4 odin REHD_Gwendlyn.mbs.v55.quad";
        // ImagePaths =
        // [
        //     "/Users/loop/Downloads/Test/ps4 odin HD_Gwendlyn.0.gnf.png",
        //     "/Users/loop/Downloads/Test/ps4 odin HD_Gwendlyn.1.gnf.png",
        //     "/Users/loop/Downloads/Test/ps4 odin HD_Gwendlyn.2.gnf.png"
        // ];
        _quadFilePath =
            @"F:\Codes\Test\ps4 odin REHD_Gwendlyn.mbs.v55.quad";
        ImagePaths =
        [
            @"F:\Codes\Test\ps4 odin HD_Gwendlyn.0.gnf.png",
            @"F:\Codes\Test\ps4 odin HD_Gwendlyn.1.gnf.png",
            @"F:\Codes\Test\ps4 odin HD_Gwendlyn.2.gnf.png"
        ];
        await LoadAsync();
    }
}