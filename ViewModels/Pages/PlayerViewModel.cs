using System.Collections;
using System.Collections.Specialized;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Skia;
using Microsoft.Extensions.DependencyInjection;
using QTSCore.Data.Quad;
using QTSCore.Process;
using QTSCore.Render;
using QTSCore.Utility;
using SkiaSharp;

namespace QTSAvalonia.ViewModels.Pages;

[SingletonService]
public partial class PlayerViewModel : ViewModelBase, IDisposable
{
#region 静态常量

    private static readonly PlayerSettingViewModel Settings =
        Instances.ServiceProvider.GetRequiredService<PlayerSettingViewModel>();

#endregion

#region 字段

    private readonly Dictionary<string, SKColor> _colorizeDict = [];
    private readonly Dictionary<string, ToggleButton> _attributesDict = [];

    private string _quadFilePath = string.Empty;

    private readonly QuadRenderer _renderer;
    private readonly AnimationPlayer _player;

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

    private static float Fps => 1 / Settings.Fps;

#endregion

#region 构造函数

    public PlayerViewModel()
    {
        _renderer = new QuadRenderer(Settings, Colorize, Attributes, _colorizeDict, _attributesDict);
        _renderer.RequestRedraw = ReDraw;

        _player = new AnimationPlayer();
        _player.SetCurrentFrame = f => CurrentFrame = f;
        _player.SetIsPlaying   = p => IsPlaying = p;

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
    public async Task LoadAsync()
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
            _renderer.QuadData = new ProcessQuadJsonFile().LoadQuadJson(quadPath);

            if (_renderer.QuadData is null)
            {
                throw new InvalidOperationException("Failed to parse Quad file");
            }

            LoggerHelper.Debug(
                $"Quad file loaded. Skeletons: {_renderer.QuadData.Skeleton?.Length ?? 0}, Animations: {_renderer.QuadData.Animation?.Length ?? 0}");
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
            _renderer.AddSourceImage(skImage);
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
        _renderer.DrawAttach(attach, Matrix.IdentityMatrixBy4X4, SKColors.Transparent);
    }

#endregion

#region 播放控制

    [RelayCommand]
    private async Task PlayAnimationAsync()
    {
        var hasAnimation = CurrentAnimation?.Timeline is not null && CurrentAnimation.Timeline.Length > 0;
        await _player.PlayAsync(CurrentFrame, TotalFrames, IsLoopAnimation, Fps, hasAnimation);
    }

    [RelayCommand]
    private void StopPlayback()
    {
        _player.StopPlayback();
    }

    [RelayCommand]
    private void SetNextFrame()
    {
        if (CurrentAnimation?.Timeline is null) return;
        CurrentFrame = _player.GetNextFrame(CurrentFrame, TotalFrames);
        LoggerHelper.Debug($"Frame changed to: {CurrentFrame}");
    }

    [RelayCommand]
    private void SetPreviousFrame()
    {
        if (CurrentAnimation?.Timeline is null) return;
        CurrentFrame = _player.GetPreviousFrame(CurrentFrame, TotalFrames);
        LoggerHelper.Debug($"Frame changed to: {CurrentFrame}");
    }

    partial void OnCurrentFrameChanged(int value)
    {
        _renderer.ClearCanvas();
        Time = value;
        _renderer.CurrentTime = value;
        _renderer.DrawSkeletonBones(CurrentSkeleton);
        Render();
    }

    private void ReDraw()
    {
        _renderer.ClearCanvas();
        _renderer.DrawSkeletonBones(CurrentSkeleton);
        Render();
    }

    private void Render()
    {
        Image = _renderer.Snapshot().ToAvaloniaImage();
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

        if (_renderer.QuadData?.Keyframe is null || attach.Id < 0 || attach.Id >= _renderer.QuadData.Keyframe.Length)
            return;

        var layers = _renderer.QuadData.Keyframe[attach.Id]?.Layers;
        if (layers is null) return;

        for (var index = 0; index < layers.Length; index++)
        {
            Layers.Add(new Button
            {
                Content = $"layer {index}", Command = DrawKeyframeLayerAttachCommand, CommandParameter = layers[index]
            });
        }

        _renderer.ClearCanvas();
        _renderer.DrawAttach(attach, Matrix.IdentityMatrixBy4X4, SKColors.Transparent);
        Render();
    }

    [RelayCommand]
    private void DrawKeyframeLayerAttach(KeyframeLayer? attach)
    {
        _renderer.ClearCanvas();
        _renderer.DrawAttach(attach, Matrix.IdentityMatrixBy4X4, SKColors.Transparent);
        Render();
    }

    [RelayCommand]
    private void DrawHitboxAttach(Attach? attach)
    {
        _renderer.ClearCanvas();
        _renderer.DrawAttach(attach, Matrix.IdentityMatrixBy4X4, SKColors.Transparent);
        Render();
    }

    private void SetSkeletons()
    {
        LoggerHelper.Debug("Setting skeletons");

        if (_renderer.QuadData?.Skeleton is null)
        {
            LoggerHelper.Warning("QuadJsonData or Skeleton is null");
            return;
        }

        Skeletons.Clear();
        foreach (var skeleton in _renderer.QuadData.Skeleton.Where(s => s != null))
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
        if (_renderer.QuadData?.Animation is null || id < 0 || id >= _renderer.QuadData.Animation.Length)
            return null;

        return _renderer.QuadData.Animation[id];
    }

    private void ClearResources()
    {
        CurrentFrame = 0;
        TotalFrames  = 0;
        Image        = null;

        Colorize.Clear();
        _colorizeDict.Clear();
        Attributes.Clear();
        _attributesDict.Clear();

        _renderer.Reset();

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

    /// <summary>
    /// 公共方法：清空所有加载的资源（供 FileManagerViewModel.ClearAll 调用）
    /// </summary>
    public void UnloadAndClear()
    {
        Clear();
        _quadFilePath = string.Empty;
        QuadFileName = string.Empty;
        ImagePaths.Clear();
    }

    /// <summary>
    /// 设置 Quad 文件路径（供 FileManagerViewModel.PreviewInPlayer 调用）
    /// </summary>
    public void SetQuadFilePath(string path)
    {
        _quadFilePath = path;
        QuadFileName = Path.GetFileName(path);
    }

    public void Dispose()
    {
        StopPlayback();
        ClearResources();
        _renderer.Dispose();

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
