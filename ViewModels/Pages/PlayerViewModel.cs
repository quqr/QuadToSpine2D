using System.Drawing;
using System.Threading;
using Avalonia.Media.Imaging;
using QTSCore.Data;
using QTSCore.Data.Quad;
using QTSCore.Process;
using QTSCore.Utility;
using SkiaSharp;

namespace QTSAvalonia.ViewModels.Pages;

public partial class PlayerViewModel : ViewModelBase, IDisposable
{
    private const int CanvasSize = 1600;
    private const float CenterX = CanvasSize / 2f;
    private const float CenterY = CanvasSize / 2f;
    private readonly Dictionary<string, Bitmap> _frameCache = new(); // Frame cache
    private readonly List<SKBitmap> _sourceImages = [];
    private readonly SKSurface _surface = SKSurface.Create(new SKImageInfo(CanvasSize, CanvasSize));

    [ObservableProperty] private ObservableCollection<Button> _animations = [];

    [ObservableProperty] private int _currentFrame;
    [ObservableProperty] private float _fps = 1f / 30f; // Default 30 FPS

    [ObservableProperty] private Bitmap? _image;


    [ObservableProperty] private ObservableCollection<string> _imagePaths = [];

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private bool _isPlaying;

    [ObservableProperty] private ObservableCollection<Button> _keyframes = [];
    private CancellationTokenSource? _playbackCancellationTokenSource;

    [ObservableProperty] private string _quadFileName = string.Empty;

    private string _quadFilePath = string.Empty;

    private QuadJsonData? _quadJsonData;

    [ObservableProperty] private ObservableCollection<Button> _skeletons = [];
    [ObservableProperty] private int _time;

    [ObservableProperty] private int _totalFrames;

    private Animation? CurrentAnimation { get; set; }
    private AnimationData? CurrentAnimationData { get; set; }
    private QuadSkeleton? CurrentSkeleton { get; set; }
    private SKCanvas Canvas => _surface.Canvas;

    private bool IsPlayingCombineAnimation { get; set; }

    public void Dispose()
    {
        StopPlayback();
        ClearResources();

        GC.SuppressFinalize(this);
    }


    private void DrawAttach(Attach? attach, Matrix matrix, Color color)
    {
        if (attach is null) return;
        switch (attach.AttachType)
        {
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
                break;
            case AttachType.List:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void DrawSkeleton(Attach attach, Matrix matrix, Color color)
    {
        foreach (var bone in _quadJsonData.Skeleton[attach.Id].Bone) DrawAttach(bone.Attach, matrix, color);
    }

    private (int currentFrameIndex, int currentTime) GetAnimationTimeIndex(int currentTime, Animation animation)
    {
        for (var index = 0; index < animation.Timeline.Count; index++)
        {
            var time = currentTime - animation.Timeline[index].Time;
            if (time < 0) return (index, -time);
        }

        return animation.IsLoop ? (-1, 0) : (animation.LoopId, currentTime);
    }

    private (Attach attach, Matrix matrix, Color color) DrawAnimation(Attach attach, Matrix matrix, Color color)
    {
        var result = (new Attach(AttachType.None, -1), matrix, color);
        var animation = _quadJsonData.Animation[attach.Id];
        var (currentFrameIndex, currentTime) = GetAnimationTimeIndex(Time, animation);
        if (currentFrameIndex < 0) return result;
        var timeline = animation.Timeline[currentFrameIndex];
        var nextFrameIndex = currentFrameIndex + 1;
        if (nextFrameIndex >= animation.Timeline.Count)
            nextFrameIndex = animation.IsLoop ? currentFrameIndex : animation.LoopId;

        var nextTimeline = animation.Timeline[nextFrameIndex];
        result.Item1 = timeline.Attach;
        if (currentFrameIndex == nextFrameIndex)
        {
            result.matrix *= timeline.AnimationMatrix;
            //TODO: Color multi
            return result;
        }

        var rate = currentTime / timeline.Time;
        var m4 = Matrix.IdentityMatrixBy4X4;
        m4 = timeline.IsMatrixMix
            ? timeline.AnimationMatrix
            : Matrix.Lerp(timeline.AnimationMatrix, nextTimeline.AnimationMatrix, rate);

        result.matrix *= m4;
        //TODO : Lerp color

        return result;
    }


    private void DrawHitBox(Attach attach, Matrix matrix, Color color)
    {
        throw new NotImplementedException();
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

            // TODO: Draw Fog

            var srcRect = SKRectI.Create((int)layer.SrcX, (int)layer.SrcY, (int)layer.Width, (int)layer.Height);
            if (layer.TexId >= _sourceImages.Count)
            {
                LoggerHelper.Error("Texture ID out of range",
                    new IndexOutOfRangeException($"Texture ID out of range: {layer.TexId}"));
                ToastHelper.Error("ERROR", $"Texture ID out of range: {layer.TexId}");
                return;
            }

            var sourceBitmap = _sourceImages[layer.TexId];
            using var croppedImage = CropBitmap(sourceBitmap, srcRect);
            if (croppedImage is null)
            {
                LoggerHelper.Error("Failed to crop image",
                    new InvalidOperationException($"Failed to crop image: {layer.TexId}"));
                ToastHelper.Error("ERROR", $"Failed to crop image: {layer.TexId}");
                return;
            }

            // Calculate transformation matrix
            var vertices = new float[8];
            var vertexMatrix = matrix * layer.DstMatrix;
            vertices = vertexMatrix.ToFloatArray();

            const int scaleFactor = 4;
            // Target points (relative to canvas center)
            var destPoints = new[]
            {
                new SKPoint(vertices[0] * scaleFactor + CenterX, vertices[1] * scaleFactor + CenterY),
                new SKPoint(vertices[2] * scaleFactor + CenterX, vertices[3] * scaleFactor + CenterY),
                new SKPoint(vertices[4] * scaleFactor + CenterX, vertices[5] * scaleFactor + CenterY),
                new SKPoint(vertices[6] * scaleFactor + CenterX, vertices[7] * scaleFactor + CenterY)
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
            using var paint = new SKPaint();
            paint.Shader = shader;
            paint.IsAntialias = true;
            paint.BlendMode = layer.BlendId > 0 ? SKBlendMode.Plus : SKBlendMode.SrcOver;

            Canvas.DrawVertices(
                verticesObj,
                SKBlendMode.SrcOver,
                paint);
        }
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
        if ((CurrentAnimation?.Timeline is null || CurrentAnimation.Timeline.Count == 0) &&
            CurrentAnimationData is null)
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
            if (!IsPlayingCombineAnimation)
                for (var i = CurrentFrame; i < CurrentAnimation.Timeline.Count && !token.IsCancellationRequested; i++)
                {
                    CurrentFrame = i;
                    await Task.Delay(TimeSpan.FromSeconds(Fps), token);
                }
            else
                for (var i = CurrentFrame;
                     i < CurrentAnimationData.Data.ElementAt(i).Value.DisplayAttachments.Count &&
                     !token.IsCancellationRequested;
                     i++)
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
        if (CurrentAnimation?.Timeline is null && CurrentAnimationData is null) return;

        if (!IsPlayingCombineAnimation)
            CurrentFrame = CurrentFrame >= CurrentAnimation.Timeline.Count - 1 ? 0 : CurrentFrame + 1;
        else
            CurrentFrame = CurrentFrame >= CurrentAnimationData.Data.Count - 1 ? 0 : CurrentFrame + 1;
        LoggerHelper.Debug($"Frame changed to: {CurrentFrame}");
    }

    [RelayCommand]
    private void SetPreviousFrame()
    {
        if (CurrentAnimation?.Timeline is null && CurrentAnimationData is null) return;

        if (!IsPlayingCombineAnimation)
            CurrentFrame = CurrentFrame <= 0 ? CurrentAnimation.Timeline.Count - 1 : CurrentFrame - 1;
        else
            CurrentFrame = CurrentFrame <= 0 ? CurrentAnimationData.Data.Count - 1 : CurrentFrame - 1;
        LoggerHelper.Debug($"Frame changed to: {CurrentFrame}");
    }

    [RelayCommand]
    private async Task OpenQuadFilePickerAsync()
    {
        LoggerHelper.Info("Opening quad file picker");
        var file = await AvaloniaFilePickerService.OpenQuadFileAsync();
        if (file?.Count > 0)
        {
            QuadFileName = file[0].Name;
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


    private Animation? GetSafeAnimation(int id)
    {
        if (_quadJsonData?.Animation is null || id < 0 || id >= _quadJsonData.Animation.Count)
            return null;
        return _quadJsonData.Animation[id];
    }



    private string GenerateCacheKey(object obj, int frameIndex)
    {
        return $"{obj.GetHashCode()}_{frameIndex}_{CurrentAnimation?.Id ?? -1}";
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

        CurrentAnimation = animation;
        TotalFrames = Math.Max(0, animation.Timeline.Count - 1);
        CurrentFrame = 0;
        var allFrames = animation.Timeline.Sum(x => x.Frames);
        Keyframes.Clear();
        foreach (var timeline in animation.Timeline.Where(t => t.Attach != null))
            Keyframes.Add(new Button
            {
                Content = $"{timeline.Attach.AttachType} {timeline.Attach.Id}", Command = DrawCommand,
                CommandParameter = attach
            });

        IsPlayingCombineAnimation = false;
        LoggerHelper.Debug($"Set {Keyframes.Count} animation timelines");
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
        Keyframes.Clear();
        CurrentSkeleton = skeleton;

        foreach (var bone in skeleton.Bone.Where(b => b.Attach is not null))
            Animations.Add(new Button
            {
                Content = $"{bone.Attach.AttachType} {bone.Attach.Id}", Command = SetKeyframesCommand,
                CommandParameter = bone.Attach
            });

        if (skeleton.Bone.Count > 1)
            Animations.Add(new Button
            {
                Content = "Combine Animations", Command = CombineAnimationsCommand
            });

        LoggerHelper.Debug($"Set {Animations.Count} animations for skeleton: {skeleton.Name}");
    }

    [RelayCommand]
    private void CombineAnimations()
    {
        //TODO
        return;
        if (CurrentSkeleton?.CombineAnimation == null) return;
        CurrentAnimationData = CurrentSkeleton.CombineAnimation;
        TotalFrames = CurrentAnimationData.Data.Count - 1;
        Keyframes.Clear();
        for (var i = 0; i < TotalFrames; i++)
            Keyframes.Add(new Button
            {
                Content = $"Frame {i}", Command = DrawCommand,
                CommandParameter = CurrentAnimationData.Data.ElementAt(i).Value.DisplayAttachments
            });

        IsPlayingCombineAnimation = true;
    }
    private void ClearResources()
    {
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
        CurrentAnimationData = null;
        CurrentSkeleton = null;
        CurrentFrame = 0;
        TotalFrames = 0;
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

        // Clear resources
        ClearResources();

        LoggerHelper.Debug("Preview data cleared");
    }
}