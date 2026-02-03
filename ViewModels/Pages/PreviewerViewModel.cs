using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
    [ObservableProperty]
    private Bitmap image;

    private List<SKBitmap> sourceImages = [];

    private List<string> sourceImagePaths =
    [
        @"F:\Codes\Test\ps4 odin HD_Gwendlyn.0.gnf.png",
        @"F:\Codes\Test\ps4 odin HD_Gwendlyn.1.gnf.png",
        @"F:\Codes\Test\ps4 odin HD_Gwendlyn.2.gnf.png"
    ];

    [ObservableProperty] private ObservableCollection<Button> _skeletons = [];
    [ObservableProperty] private ObservableCollection<Button> _animations = [];
    [ObservableProperty] private ObservableCollection<Button> _animationTimelines = [];

    private QuadJsonData _quadJsonData;

    [RelayCommand]
    private void Load()
    {
        Image = LoadBitmap(@"F:\Codes\Test\ps4 odin HD_Gwendlyn.2.gnf.png");
        

        
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            LoadQuadFile(@"F:\Codes\Test\ps4 odin REHD_Gwendlyn.mbs.v55.quad");
            SetSkeletons();
        });
        foreach (var path in sourceImagePaths)
        {
            using var stream  = File.OpenRead(path);
            var       skImage = SKBitmap.Decode(stream);
            sourceImages.Add(skImage);
        }
    }

    public static Bitmap LoadBitmap(string imagePath)
    {
        using var stream = File.OpenRead(imagePath);
        using var image  = SKImage.FromBitmap(SKBitmap.Decode(stream));
        
        var       imageInfo = new SKImageInfo(image.Width, image.Height); 
        using var surface   = SKSurface.Create(imageInfo);
        using var canvas    = surface.Canvas;
        
        using var shader = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
        using var paint  = new SKPaint();
        paint.Shader = shader;
        var verts = new[]
        {
            new SKPoint(0, 0),
            new SKPoint()
        }
        canvas.DrawVertices(SKVertexMode.Triangles,[],[],paint);
        var snapshot = surface.Snapshot();
        var a = SKBitmap.FromImage(snapshot)?.ToBitmap();
        snapshot.Dispose();
        
        
        return new Bitmap(stream);
    }

    private void LoadQuadFile(string quadPath)
    {
        var json = File.ReadAllText(quadPath);
        _quadJsonData = JsonConvert.DeserializeObject<QuadJsonData>(json);
    }

    [RelayCommand]
    void SetAnimationTimelines(Attach attach)
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
        {
            Animations.Add(new Button
            {
                Content = $"{bone.Attach.AttachType} {bone.Attach.Id}", Command = SetAnimationTimelinesCommand, CommandParameter = bone.Attach
            });
        }
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
    private void GetFrame(Timeline timeline)
    {
        var       attach    = timeline.Attach;
        var       imageInfo = new SKImageInfo(800, 600); 
        using var surface   = SKSurface.Create(imageInfo);
        using var canvas    = surface.Canvas;
        float     centerX   = imageInfo.Width  / 2f;
        float     centerY   = imageInfo.Height / 2f;
        switch (attach.AttachType)
        {
            case AttachType.Keyframe:
                var keyframe = _quadJsonData.Keyframe[attach.Id];
                if (keyframe?.Layers is null) return;
                foreach (var layer in keyframe.Layers)
                {
                    if (layer?.Srcquad == null || layer.TexId < 0) continue;
                    var       src       = SKRectI.Create((int)layer.SrcX, (int)layer.SrcY, (int)layer.Width, (int)layer.Height);
                    var       source    = sourceImages[layer.TexId];
                    using var cropImage = CropBitmap(source, src);
                    
                    var vert = AnimationMatrixUtility.QuadMultiply(timeline.AnimationMatrix, layer.DstMatrix);
                    var x = ProcessUtility.MinusFloats(vert.ToFloatArray(), layer.ZeroCenterPoints);
                    
                    var destX = imageInfo.Width  / 2f - layer.DstX;
                    var destY = imageInfo.Height / 2f - layer.DstY;
                    
                   
                    
                    using var image  = SKImage.FromBitmap(cropImage);
                    using var shader = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
                    using var paint  = new SKPaint();
                    paint.Shader = shader;
                    canvas.DrawVertices(SKVertexMode.Triangles,[],[],paint);
                }
            
                // 生成最终图像
                var snapshot = surface.Snapshot();
                Image = SKBitmap.FromImage(snapshot)?.ToBitmap();
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
        image.Dispose();
    }
}