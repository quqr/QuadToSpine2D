using System.Threading;
using Avalonia.Media.Imaging;
using QTSCore.Data.Quad;
using QTSCore.Utility;
using SkiaSharp;

namespace QTSAvalonia.ViewModels.Pages;

[Service]
public partial class PreviewerViewModel : ViewModelBase, IDisposable
{
#region Fields

    private readonly List<SKBitmap> _sourceImages = [];
    private readonly Dictionary<string, Bitmap> _frameCache = new(); // Frame cache
    private CancellationTokenSource? _playbackCancellationTokenSource;

#endregion

#region Properties

    [ObservableProperty]
    private ObservableCollection<string> _imagePaths = [];

    [ObservableProperty]
    private ObservableCollection<Button> _animations = [];

    [ObservableProperty]
    private ObservableCollection<Button> _animationTimelines = [];

    private Animation? _currentAnimation;
    private QuadSkeleton? _currentSkeleton;
    private Timeline? _currentTimeline;

    [ObservableProperty]
    private int _currentFrame;

    [ObservableProperty]
    private float _fps = 1f / 30f; // Default 30 FPS

    [ObservableProperty]
    private Bitmap? _image;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isPlaying;

    private QuadJsonData? _quadJsonData;

    [ObservableProperty]
    private ObservableCollection<Button> _skeletons = [];

    [ObservableProperty]
    private int _totalFrames;

    private string _quadFilePath = string.Empty;

    [ObservableProperty]
    private string _quadFileName = string.Empty;

#endregion


    public void Dispose()
    {

        StopPlayback();
        ClearResources();

        GC.SuppressFinalize(this);
    }

    private void ClearResources()
    {
        // Clean up image resources
        Image?.Dispose();
        Image = null;
        foreach (var bitmap in _sourceImages)
        {
            bitmap?.Dispose();
        }

        _sourceImages.Clear();

        // Clean up cache
        foreach (var (_, cachedBitmap) in _frameCache)
        {
            cachedBitmap?.Dispose();
        }

        _frameCache.Clear();

        // Clean up data
        _quadJsonData     = null;
        _currentAnimation = null;
        _currentSkeleton  = null;
        _currentTimeline  = null;
        CurrentFrame      = 0;
        TotalFrames       = 0;

    }

#region Public Commands

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

    [RelayCommand]
    private async Task PlayAnimationAsync()
    {
        if (_currentAnimation?.Timeline is null || _currentAnimation.Timeline.Count == 0)
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
            for (var i = CurrentFrame; i < _currentAnimation.Timeline.Count && !token.IsCancellationRequested; i++)
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
        if (_currentAnimation?.Timeline is null) return;

        CurrentFrame = CurrentFrame >= _currentAnimation.Timeline.Count - 1 ? 0 : CurrentFrame + 1;
        LoggerHelper.Debug($"Frame changed to: {CurrentFrame}");
    }

    [RelayCommand]
    private void SetPreviousFrame()
    {
        if (_currentAnimation?.Timeline is null) return;

        CurrentFrame = CurrentFrame <= 0 ? _currentAnimation.Timeline.Count - 1 : CurrentFrame - 1;
        LoggerHelper.Debug($"Frame changed to: {CurrentFrame}");
    }

    [RelayCommand]
    private async Task OpenQuadFilePickerAsync()
    {
        LoggerHelper.Info("Opening quad file picker");
        var file = await AvaloniaFilePickerService.OpenQuadFileAsync();
        if (file?.Count > 0)
        {
            QuadFileName  = file[0].Name;
            _quadFilePath = Uri.UnescapeDataString(file[0].Path.AbsolutePath);
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
                var imagePath = Uri.UnescapeDataString(file.Path.AbsolutePath);
                ImagePaths.Add(imagePath);
                LoggerHelper.Debug($"Added image path: {imagePath}");
            }
            LoggerHelper.Info($"Selected {ImagePaths.Count} image files");
            ToastHelper.Success("SUCCESS", $"Selected {ImagePaths.Count} image files");
        }
    }

#endregion

#region Animation Navigation Commands

    [RelayCommand]
    private void SetAnimationTimelines(Attach? attach)
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

        _currentAnimation = animation;
        TotalFrames       = Math.Max(0, animation.Timeline.Count - 1);
        CurrentFrame      = 0;

        AnimationTimelines.Clear();
        foreach (var timeline in animation.Timeline.Where(t => t.Attach != null))
        {
            AnimationTimelines.Add(new Button
            {
                Content = $"{timeline.Attach.AttachType} {timeline.Attach.Id}", Command = SetAttachCommand, CommandParameter = timeline
            });
        }

        LoggerHelper.Debug($"Set {AnimationTimelines.Count} animation timelines");
    }

    [RelayCommand]
    private void SetAnimations(QuadSkeleton? skeleton)
    {
        if (skeleton?.Bone is null)
        {
            LoggerHelper.Warn("Invalid skeleton or no bones");
            return;
        }

        Animations.Clear();
        AnimationTimelines.Clear();
        _currentSkeleton = skeleton;

        foreach (var bone in skeleton.Bone.Where(b => b.Attach != null))
        {
            Animations.Add(new Button
            {
                Content = $"{bone.Attach.AttachType} {bone.Attach.Id}", Command = SetAnimationTimelinesCommand, CommandParameter = bone.Attach
            });
        }

        LoggerHelper.Debug($"Set {Animations.Count} animations for skeleton: {skeleton.Name}");
    }

    [RelayCommand]
    private void SetAttach(Timeline? timeline)
    {
        if (timeline?.Attach is null)
        {
            LoggerHelper.Warn("Invalid timeline or attach");
            return;
        }

        var cacheKey = GenerateCacheKey(timeline, CurrentFrame);
        if (_frameCache.TryGetValue(cacheKey, out var cachedBitmap))
        {
            Image = cachedBitmap;
            LoggerHelper.Debug($"Using cached frame: {cacheKey}");
            return;
        }

        var bitmap = GetAttach(timeline);
        if (bitmap is null)
        {
            LoggerHelper.Warn("Failed to generate attach bitmap");
            return;
        }

        // Cache newly generated frame
        _frameCache[cacheKey] = bitmap;
        Image                 = bitmap;

        // Limit cache size to prevent memory leaks
        if (_frameCache.Count > 1000)
        {
            var oldestKey = _frameCache.Keys.First();
            _frameCache[oldestKey]?.Dispose();
            _frameCache.Remove(oldestKey);
        }

        LoggerHelper.Debug($"Generated and cached new frame: {cacheKey}");
    }

#endregion

#region Private Methods

    [RelayCommand]
    private void Clear()
    {
        LoggerHelper.Debug("Clearing preview data");

        StopPlayback();

        // Clear UI collections
        Skeletons.Clear();
        Animations.Clear();
        AnimationTimelines.Clear();

        // Clear resources
        ClearResources();

        LoggerHelper.Debug("Preview data cleared");
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
            var json = File.ReadAllText(quadPath);
            _quadJsonData = JsonConvert.DeserializeObject<QuadJsonData>(json);

            if (_quadJsonData is null)
            {
                LoggerHelper.Error("Failed to parse Quad file", new InvalidOperationException("Failed to parse Quad file"));
                ToastHelper.Error("ERROR", "Failed to parse Quad file");
                return;
            }

            LoggerHelper.Debug($"Quad file loaded. Skeletons: {_quadJsonData.Skeleton?.Count ?? 0}, Animations: {_quadJsonData.Animation?.Count ?? 0}");
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
                await using var stream  = File.OpenRead(path);
                var             skImage = SKBitmap.Decode(stream);
                if (skImage is null)
                {
                    LoggerHelper.Error("Cannot decode image", new InvalidOperationException($"Cannot decode image: {path}"));
                    ToastHelper.Error("ERROR", $"Cannot decode image: {path}");
                    return;
                }

                _sourceImages.Add(skImage);
                LoggerHelper.Debug($"Loaded image: {path}");
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Failed to load image", new InvalidOperationException($"Failed to load image: {path}", ex));
                ToastHelper.Error("ERROR", $"Failed to load image: {path}");
                return;
            }
        });

        await Task.WhenAll(loadTasks);
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
        {
            Skeletons.Add(new Button
            {
                Content = skeleton.Name, Command = SetAnimationsCommand, CommandParameter = skeleton
            });
        }

        LoggerHelper.Debug($"Set {Skeletons.Count} skeletons");
    }

    private Bitmap? GetAttach(Timeline timeline)
    {
        var attach = timeline.Attach;
        return attach?.AttachType switch
        {
            AttachType.Keyframe => GetKeyframe(timeline, attach),
            _                   => null
        };
    }

    private Bitmap? GetKeyframe(Timeline timeline, Attach attach)
    {
        var keyframe = GetSafeKeyframe(attach.Id);
        if (keyframe?.Layers is null || keyframe.Layers.Count == 0)
        {
            LoggerHelper.Warn($"Invalid keyframe or no layers: ID {attach.Id}");
            return null;
        }

        const int canvasSize = 1600;
        var       imageInfo  = new SKImageInfo(canvasSize, canvasSize);

        using var surface = SKSurface.Create(imageInfo);
        using var canvas  = surface.Canvas;

        const float centerX = canvasSize / 2f;
        const float centerY = canvasSize / 2f;

        // Clear canvas
        canvas.Clear(SKColors.Transparent);

        foreach (var layer in keyframe.Layers.Where(l => l is { Srcquad: not null, TexId: >= 0 }))
        {
            try
            {
                RenderLayer(canvas, timeline, layer, centerX, centerY);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"Failed to render layer: TexId={layer.TexId}", ex);
                ToastHelper.Error("ERROR", $"Failed to render layer: {layer.TexId}");
                return null;
            }
        }

        using var snapshot = surface.Snapshot();
        return SKBitmap.FromImage(snapshot)?.ToBitmap();
    }

    private void RenderLayer(SKCanvas canvas, Timeline timeline, KeyframeLayer layer, float centerX, float centerY)
    {
        // Crop source image
        var srcRect = SKRectI.Create((int)layer.SrcX, (int)layer.SrcY, (int)layer.Width, (int)layer.Height);
        if (layer.TexId >= _sourceImages.Count)
        {
            LoggerHelper.Error("Texture ID out of range", new IndexOutOfRangeException($"Texture ID out of range: {layer.TexId}"));
            ToastHelper.Error("ERROR", $"Texture ID out of range: {layer.TexId}");
            return;
        }

        var       sourceBitmap = _sourceImages[layer.TexId];
        using var croppedImage = CropBitmap(sourceBitmap, srcRect);
        if (croppedImage is null)
        {
            LoggerHelper.Error("Failed to crop image", new InvalidOperationException($"Failed to crop image: {layer.TexId}"));
            ToastHelper.Error("ERROR", $"Failed to crop image: {layer.TexId}");
            return;
        }

        // Calculate transformation matrix
        var vertexMatrix = AnimationMatrixUtility.QuadMultiply(timeline.AnimationMatrix, layer.DstMatrix);
        var vertices     = vertexMatrix.ToFloatArray();

        const int scaleFactor = 4;
        // Target points (relative to canvas center)
        var destPoints = new[]
        {
            new SKPoint(vertices[0] * scaleFactor + centerX, vertices[1] * scaleFactor + centerY),
            new SKPoint(vertices[2] * scaleFactor + centerX, vertices[3] * scaleFactor + centerY),
            new SKPoint(vertices[4] * scaleFactor + centerX, vertices[5] * scaleFactor + centerY),
            new SKPoint(vertices[6] * scaleFactor + centerX, vertices[7] * scaleFactor + centerY)
        };

        // Texture coordinates (relative to crop area)
        var texturePoints = new[]
        {
            new SKPoint(layer.Srcquad[0] - layer.SrcX, layer.Srcquad[1] - layer.SrcY),
            new SKPoint(layer.Srcquad[2] - layer.SrcX, layer.Srcquad[3] - layer.SrcY),
            new SKPoint(layer.Srcquad[4] - layer.SrcX, layer.Srcquad[5] - layer.SrcY),
            new SKPoint(layer.Srcquad[6] - layer.SrcX, layer.Srcquad[7] - layer.SrcY)
        };

        // Triangle indices
        ushort[] indices = [0, 1, 2, 0, 2, 3];

        using var verticesObj = SKVertices.CreateCopy(
            SKVertexMode.Triangles,
            destPoints,
            texturePoints,
            null,
            indices);

        using var shader = SKShader.CreateBitmap(croppedImage);
        using var paint  = new SKPaint();
        paint.Shader      = shader;
        paint.IsAntialias = true;
        paint.BlendMode   = layer.BlendId > 0 ? SKBlendMode.Plus : SKBlendMode.SrcOver;

        canvas.DrawVertices(
            verticesObj,
            SKBlendMode.SrcOver,
            paint);
    }

    private SKBitmap? CropBitmap(SKBitmap? source, SKRectI srcRect)
    {
        if (source == null || srcRect.Width <= 0 || srcRect.Height <= 0)
            return null;

        // Boundary protection
        var safeRect = SKRectI.Intersect(srcRect, new SKRectI(0, 0, source.Width, source.Height));
        if (safeRect.Width <= 0 || safeRect.Height <= 0)
            return null;

        var       cropped = new SKBitmap(safeRect.Width, safeRect.Height);
        using var canvas  = new SKCanvas(cropped);
        canvas.DrawBitmap(source, -safeRect.Left, -safeRect.Top);

        return cropped;
    }

    private Animation? GetSafeAnimation(int id)
    {
        if (_quadJsonData?.Animation is null || id < 0 || id >= _quadJsonData.Animation.Count)
            return null;
        return _quadJsonData.Animation[id];
    }

    private Keyframe? GetSafeKeyframe(int id)
    {
        if (_quadJsonData?.Keyframe is null || id < 0 || id >= _quadJsonData.Keyframe.Count)
            return null;
        return _quadJsonData.Keyframe[id];
    }

    private string GenerateCacheKey(Timeline timeline, int frameIndex)
    {
        return $"{timeline.GetHashCode()}_{frameIndex}_{_currentAnimation?.Id ?? -1}";
    }

#endregion

#region Property Changed Handlers

    partial void OnCurrentFrameChanged(int value)
    {
        LoggerHelper.Debug($"Current frame changed to: {value}");

        if (_currentAnimation?.Timeline is null || value < 0 || value >= _currentAnimation.Timeline.Count)
        {
            LoggerHelper.Warn($"Invalid frame index: {value}");
            return;
        }

        var timeline = _currentAnimation.Timeline[value];
        SetAttachCommand.Execute(timeline);
    }

    partial void OnFpsChanged(float value)
    {
        LoggerHelper.Debug($"FPS changed to: {value}");
        // Add real-time adjustment for playback speed
    }

#endregion
}