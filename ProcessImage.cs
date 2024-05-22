﻿using System.Numerics;
using SixLabors.ImageSharp.Processing;

namespace QuadPlayer;
using SixLabors.ImageSharp;

public class ProcessImage
{
    public Dictionary<string, ImageData> ImagesData { get; set; }= new();
    private int _imageNums;
    private int _imageIndex;
    public ProcessImage(string[] imagePath,QuadJson quad)
    {
        Console.WriteLine("Cutting images...");
        for (var index = 0; index < imagePath.Length; index++)
        {
            _imageNums = index;
            using var image = Image.Load(imagePath[index]);
            foreach (var keyframe in quad.Keyframe)
            {
                foreach (var layer in keyframe.Layer)
                {
                    if (ImagesData.ContainsKey(layer.LayerGuid))
                    {
                        layer.LayerName = ImagesData[layer.LayerGuid].ImageName;
                        continue;
                    }
                    ImagesData[layer.LayerGuid] = CutImage(image, CalculateRectangle(layer), layer);
                }
            }
        }
        Console.WriteLine("Finished");
    }
    private Rectangle CalculateRectangle(KeyframeLayer layer)
    {
        return new Rectangle()
        {
            X = (int)layer.MinAndMaxSrcPoints[0],
            Y = (int)layer.MinAndMaxSrcPoints[1],
            Width = (int)layer.Width,
            Height = (int)layer.Height,
        };
    }

    private ImageData CutImage(Image image,Rectangle rectangle,KeyframeLayer layer)
    {
        using var cutImage = image.Clone(context =>
        {
            context.Crop(rectangle);
        });
        var imageName = $"Slice {_imageNums}_{_imageIndex}";
        layer.LayerName = imageName;
        cutImage.SaveAsPng($"D:\\Download\\quad_mobile_v05_beta-20240404-2000\\quad_mobile_v05_beta\\data\\Output\\{imageName}.png");
        _imageIndex++;
        return new ImageData
        {
            UVs = layer.UVs,
            Width = cutImage.Width,
            Height = cutImage.Height,
            SrcVertices = layer.Srcquad,
            ImageName = imageName,
            ZeroCenterPoints = layer.ZeroCenterPoints,
        };
    }
}

public class ImageData
{
    public float[] UVs { get; set; } = new float[8];
    public float Width { get; set; }
    public float Height { get; set; }
    public float[] SrcVertices { get; set; } = new float[8];
    public string ImageName { get; set; } = string.Empty;
    public float[] ZeroCenterPoints { get; set; }
}