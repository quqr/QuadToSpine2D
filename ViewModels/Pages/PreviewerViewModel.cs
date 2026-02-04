using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using QTSAvalonia.Utilities;
using QTSCore.Data.Quad;
using QTSCore.Utility;
using SkiaSharp;

namespace QTSAvalonia.ViewModels.Pages;

public partial class PreviewerViewModel : ViewModelBase, IDisposable
{
    private readonly List<string> _sourceImagePaths =
    [
        @"F:\Codes\Test\ps4 odin HD_Gwendlyn.0.gnf.png",
        @"F:\Codes\Test\ps4 odin HD_Gwendlyn.1.gnf.png",
        @"F:\Codes\Test\ps4 odin HD_Gwendlyn.2.gnf.png"
    ];

    private readonly List<SKBitmap> _sourceImages = [];

    private readonly List<string> imagePaths = [];
    [ObservableProperty] private ObservableCollection<Button> _animations = [];
    [ObservableProperty] private ObservableCollection<Button> _animationTimelines = [];
    private Animation? _currentAnimation;

    [ObservableProperty]
    private int _currentFrame;

    private QuadSkeleton? _currentSkeleton;
    private Timeline? _currentTimeline;

    [ObservableProperty] private float _fps = 1 / 30f;
    [ObservableProperty] private Bitmap _image = null!;
    private string _quadFilePath;

    private QuadJsonData _quadJsonData = null!;
    [ObservableProperty] private ObservableCollection<Button> _skeletons = [];

    [ObservableProperty] private int _totalFrames;

    public string QuadFileName { get; set; }

    public void Dispose()
    {
        Image.Dispose();
    }

    [RelayCommand]
    private void Load()
    {
        Clear();
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            LoadQuadFile(@"F:\Codes\Test\ps4 odin REHD_Gwendlyn.mbs.v55.quad");
            SetSkeletons();
        });
        foreach (var path in _sourceImagePaths)
        {
            using var stream  = File.OpenRead(path);
            var       skImage = SKBitmap.Decode(stream);
            _sourceImages.Add(skImage);
        }
    }

    private void Clear()
    {

        Skeletons?.Clear();
        Animations?.Clear();
        AnimationTimelines?.Clear();
        _sourceImages?.Clear();
        Image?.Dispose();
        _quadJsonData     = null!;
        _currentAnimation = null;
        _currentSkeleton  = null;
        _currentTimeline  = null;
    }

    private void LoadQuadFile(string quadPath)
    {
        var json = File.ReadAllText(quadPath);
        _quadJsonData = JsonConvert.DeserializeObject<QuadJsonData>(json)!;
    }

    [RelayCommand]
    private void SetAnimationTimelines(Attach attach)
    {
        if (attach.AttachType != AttachType.Animation) return;
        var animation = _quadJsonData.Animation[attach.Id];
        if (animation is null) return;
        AnimationTimelines.Clear();

        _currentAnimation = animation;
        TotalFrames       = animation.Timeline.Count - 1;

        foreach (var timeline in animation.Timeline)
        {
            if (timeline.Attach is null) return;
            AnimationTimelines.Add(new Button
            {
                Content = $"{timeline.Attach.AttachType} {timeline.Attach.Id}", Command = SetAttachCommand, CommandParameter = timeline
            });
        }
    }

    [RelayCommand]
    private void SetAnimations(QuadSkeleton skeleton)
    {
        if (skeleton.Bone is null) return;
        Animations.Clear();
        AnimationTimelines.Clear();
        _currentSkeleton = skeleton;
        foreach (var bone in skeleton.Bone)
            Animations.Add(new Button
            {
                Content = $"{bone.Attach.AttachType} {bone.Attach.Id}", Command = SetAnimationTimelinesCommand, CommandParameter = bone.Attach
            });
    }

    private void SetSkeletons()
    {
        foreach (var skeleton in _quadJsonData.Skeleton)
        {
            if (skeleton is null) continue;
            Skeletons.Add(new Button
            {
                Content = skeleton.Name, Command = SetAnimationsCommand, CommandParameter = skeleton
            });
        }
    }

    private SKBitmap? CropBitmap(SKBitmap? source, SKRectI srcRect)
    {
        if (source == null || srcRect.Width <= 0 || srcRect.Height <= 0)
            return null;

        // 边界保护：防止裁剪区域超出源图范围
        var safeRect = SKRectI.Intersect(srcRect, new SKRectI(0, 0, source.Width, source.Height));
        if (safeRect.Width <= 0 || safeRect.Height <= 0)
            return null;
        var       cropped = new SKBitmap(safeRect.Width, safeRect.Height);
        using var canvas  = new SKCanvas(cropped);

        canvas.DrawBitmap(source, -safeRect.Left, -safeRect.Top);
        return cropped;
    }

    [RelayCommand]
    private void SetAttach(Timeline timeline)
    {
        var bitmap = GetAttach(timeline);
        if (bitmap is null) return;
        Image = bitmap;
    }

    private Bitmap? GetAttach(Timeline timeline)
    {
        var attach = timeline.Attach;
        if (attach is null) return null;
        var bitmap = attach.AttachType switch
        {
            AttachType.Keyframe => GetKeyframe(timeline, attach),
            _                   => null
        };
        return bitmap;
    }

    private Bitmap? GetKeyframe(Timeline timeline, Attach attach)
    {

        var keyframe = _quadJsonData.Keyframe[attach.Id];
        if (keyframe?.Layers is null) return null;
        var       imageInfo = new SKImageInfo(800, 800);
        using var surface   = SKSurface.Create(imageInfo);
        using var canvas    = surface.Canvas;
        var       centerX   = imageInfo.Width  / 2f;
        var       centerY   = imageInfo.Height / 2f;
        foreach (var layer in keyframe.Layers)
        {
            if (layer?.Srcquad == null || layer.TexId < 0) continue;
            var       src        = SKRectI.Create((int)layer.SrcX, (int)layer.SrcY, (int)layer.Width, (int)layer.Height);
            var       bitmap     = _sourceImages[layer.TexId];
            using var cropImage  = CropBitmap(bitmap, src);
            var       vertMatrix = AnimationMatrixUtility.QuadMultiply(timeline.AnimationMatrix, layer.DstMatrix);
            var       verts      = vertMatrix.ToFloatArray();
            SKPoint[] destPoints =
            [
                new(verts[0] + centerX, verts[1] + centerY),
                new(verts[2] + centerX, verts[3] + centerY),
                new(verts[4] + centerX, verts[5] + centerY),
                new(verts[6] + centerX, verts[7] + centerY)
            ];

            var srcQuad = layer.Srcquad;
            var srcPoints = new[]
            {
                new SKPoint(srcQuad[0], srcQuad[1]),
                new SKPoint(srcQuad[2], srcQuad[3]),
                new SKPoint(srcQuad[4], srcQuad[5]),
                new SKPoint(srcQuad[6], srcQuad[7])
            };

            // 重新排序纹理坐标以匹配标准UV映射
            var texturePoints = new SKPoint[4];
            for (var i = 0; i < 4; i++)
            {
                var point = srcPoints[i];
                // 转换为相对于裁剪区域的坐标
                var localX = point.X - layer.SrcX;
                var localY = point.Y - layer.SrcY;
                texturePoints[i] = new SKPoint(localX, localY);
            }

            // 创建三角形索引（两个三角形组成四边形）
            ushort[] indices = [0, 1, 2, 0, 2, 3]; // 三角形1: 0-1-2, 三角形2: 0-2-3

            using var vertices = SKVertices.CreateCopy(
                SKVertexMode.Triangles,
                destPoints,
                texturePoints,
                null,
                indices);
            using var shader = SKShader.CreateBitmap(cropImage);
            using var paint  = new SKPaint();
            paint.Shader      = shader;
            paint.IsAntialias = true;
            canvas.DrawVertices(vertices, layer.BlendId > 0 ? SKBlendMode.Plus : SKBlendMode.SrcOver, paint);
        }

        using var snapshot = surface.Snapshot();
        return SKBitmap.FromImage(snapshot).ToBitmap();
    }

    [RelayCommand]
    private async Task PlayAnimationAsync()
    {
        if (_currentAnimation is null) return;
        for (var i = CurrentFrame; i < _currentAnimation.Timeline.Count; i++)
        {
            CurrentFrame++;
            await Task.Delay((int)(1000 * Fps));
        }
    }

    [RelayCommand]
    private void SetNextFrame()
    {

        if (CurrentFrame >= _currentAnimation?.Timeline.Count)
        {
            CurrentFrame = 0;
        }
        else
        {
            CurrentFrame++;
        }
    }

    [RelayCommand]
    private void SetPreviousFrame()
    {
        if (_currentAnimation is null) return;
        if (CurrentFrame <= 0)
        {
            CurrentFrame = _currentAnimation.Timeline.Count - 1;
        }
        else
        {
            CurrentFrame--;
        }
    }

    partial void OnCurrentFrameChanged(int value)
    {
        if (CurrentFrame < _currentAnimation?.Timeline.Count)
        {
            var bitmap = GetAttach(_currentAnimation.Timeline[value]);
            if (bitmap is null) return;
            Image = bitmap;
        }
    }

    [RelayCommand]
    private async Task OpenQuadFilePicker()
    {
        var file = await InstanceSingleton.Instance.FilePickerService.OpenQuadFileAsync();
        if (file is not null)
        {
            QuadFileName  = file[0].Name;
            _quadFilePath = Uri.UnescapeDataString(file[0].Path.AbsolutePath);
        }
    }

    [RelayCommand]
    private async Task OpenImageFilePicker()
    {
        var file = await InstanceSingleton.Instance.FilePickerService.OpenImageFilesAsync();
        if (file is not null)
        {
            imagePaths.AddRange(file.Select(f => Uri.UnescapeDataString(f.Path.AbsolutePath)));
        }
    }
}