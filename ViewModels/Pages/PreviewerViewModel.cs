using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
    [ObservableProperty] private Bitmap _image = null!;

    private List<SKBitmap> _sourceImages = [];


    private List<string> _sourceImagePaths =
    [
        //@"F:\Codes\Test\ps4 odin HD_Gwendlyn.0.gnf.png",
        //@"F:\Codes\Test\ps4 odin HD_Gwendlyn.1.gnf.png",
        //@"F:\Codes\Test\ps4 odin HD_Gwendlyn.2.gnf.png"
    ];

    [ObservableProperty] private ObservableCollection<Button> _skeletons = [];
    [ObservableProperty] private ObservableCollection<Button> _animations = [];
    [ObservableProperty] private ObservableCollection<Button> _animationTimelines = [];


    private QuadJsonData _quadJsonData = null!;

    private void LoadImageIntoControl(SKData skData)
    {
        using var skImage = SKImage.FromEncodedData(skData);
        // 将 SkiaSharp 的图像转换为 Avalonia 的 IBitmap
        using var skBitmap = SKBitmap.FromImage(skImage);
        using var ms = new MemoryStream();
        // 保存 SKBitmap 到内存流
        skBitmap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(ms);
        ms.Seek(0, SeekOrigin.Begin);
        Image = new Bitmap(ms);
    }

    [RelayCommand]
    private void Load()
    {
        LoadBitmap(@"/Users/loop/Downloads/pictures/ggg.jpg");

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            //LoadQuadFile(@"F:\Codes\Test\ps4 odin REHD_Gwendlyn.mbs.v55.quad");
            //SetSkeletons();
        });
        foreach (var path in _sourceImagePaths)
        {
            using var stream = File.OpenRead(path);
            var skImage = SKBitmap.Decode(stream);
            _sourceImages.Add(skImage);
        }
    }

    private void LoadBitmap(string imagePath)
    {
        var bitmap = SKBitmap.Decode(imagePath);

        //Test points
        SKPoint[] destPoints =
        [
            new(0, 0), // 左上
            new(700, 120), // 右上（下移40px）
            new(650, 480), // 右下（上移20px）
            new(150, 520) // 左下（下移20px）
        ];
        // 2. 定义原始图片的四个角点（纹理坐标）
        SKPoint[] texturePoints =
        [
            new(0, 0), // 左上角
            new(bitmap.Width, 0), // 右上角
            new(bitmap.Width, bitmap.Height), // 右下角
            new(0, bitmap.Height) // 左下角
        ];

        // 3. 创建三角形索引（两个三角形组成四边形）
        ushort[] indices = [0, 1, 2, 0, 2, 3]; // 三角形1: 0-1-2, 三角形2: 0-2-3

        // 4. 创建顶点集合 - 修复参数类型问题
        using var vertices = SKVertices.CreateCopy(
            SKVertexMode.Triangles,
            destPoints,
            texturePoints,
            null,
            indices);

        // 5. 创建着色器（绑定图片）
        using var shader = SKShader.CreateBitmap(bitmap, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
        using var paint = new SKPaint();
        paint.Shader = shader;
        paint.IsAntialias = true;

        // 6. 创建画布并绘制
        var info = new SKImageInfo(800, 600);
        using (var surface = SKSurface.Create(info))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            // 绘制变形后的图片
            canvas.DrawVertices(vertices, SKBlendMode.SrcOver, paint);

            using var image = surface.Snapshot();
            var data = image.Encode(SKEncodedImageFormat.Png, 100);
            LoadImageIntoControl(data);
        }

        bitmap.Dispose();
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
        foreach (var timeline in animation.Timeline)
        {
            if (timeline.Attach is null) return;
            AnimationTimelines.Add(new Button
            {
                Content = $"{timeline.Attach.AttachType} {timeline.Attach.Id} {animation.IsLoop}",
                Command = GetFrameCommand,
                CommandParameter = timeline
            });
        }
    }

    [RelayCommand]
    private void SetAnimations(QuadSkeleton skeleton)
    {
        if (skeleton.Bone is null) return;
        Animations.Clear();
        AnimationTimelines.Clear();
        foreach (var bone in skeleton.Bone)
            Animations.Add(new Button
            {
                Content = $"{bone.Attach.AttachType} {bone.Attach.Id}", Command = SetAnimationTimelinesCommand,
                CommandParameter = bone.Attach
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
        var cropped = new SKBitmap(safeRect.Width, safeRect.Height);
        using var canvas = new SKCanvas(cropped);

        canvas.DrawBitmap(source, -safeRect.Left, -safeRect.Top);
        return cropped;
    }

    [RelayCommand]
    private void GetFrame(Timeline timeline)
    {
        var attach = timeline.Attach;
        var imageInfo = new SKImageInfo(800, 600);
        using var surface = SKSurface.Create(imageInfo);
        using var canvas = surface.Canvas;
        var centerX = imageInfo.Width / 2f;
        var centerY = imageInfo.Height / 2f;
        switch (attach.AttachType)
        {
            case AttachType.Keyframe:
                var keyframe = _quadJsonData.Keyframe[attach.Id];
                if (keyframe?.Layers is null) return;
                foreach (var layer in keyframe.Layers)
                {
                    if (layer?.Srcquad == null || layer.TexId < 0) continue;
                    var src = SKRectI.Create((int)layer.SrcX, (int)layer.SrcY, (int)layer.Width, (int)layer.Height);
                    var source = _sourceImages[layer.TexId];
                    using var cropImage = CropBitmap(source, src);

                    var vert = AnimationMatrixUtility.QuadMultiply(timeline.AnimationMatrix, layer.DstMatrix);
                    var x = ProcessUtility.MinusFloats(vert.ToFloatArray(), layer.ZeroCenterPoints);

                    var destX = imageInfo.Width / 2f - layer.DstX;
                    var destY = imageInfo.Height / 2f - layer.DstY;
                    //TODO: 

                    using var image = SKImage.FromBitmap(cropImage);
                    using var shader = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
                    using var paint = new SKPaint();
                    paint.Shader = shader;
                    canvas.DrawVertices(SKVertexMode.Triangles, [], [], paint);
                }

                var snapshot = surface.Snapshot();
                Image = SKBitmap.FromImage(snapshot).ToBitmap();
                snapshot.Dispose();
                break;
            case AttachType.Slot:
            case AttachType.HitBox:
            case AttachType.Animation:
            case AttachType.Skeleton:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [RelayCommand]
    private void PlayAnimation()
    {
    }

    [RelayCommand]
    private void SetNextFrame()
    {
    }

    [RelayCommand]
    private void SetPreviousFrame()
    {
    }

    public void Dispose()
    {
        _image.Dispose();
    }
}