using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace QuadPlayer;
using SixLabors.ImageSharp;

public class ProcessImage
{
    public Dictionary<string, Image> ClipImage = new();

    public ProcessImage(QuadJson quad)
    {
        Image image = ReadImage("D:\\Code\\C#\\Q\\QuadPlayer\\src\\swi sent Amiguchi00.0.nvt.png");
        foreach (var keyframe in quad.Keyframe)
        {
            if(keyframe.Layers is null) continue;
            foreach (var layer in keyframe.Layers)
            {
                if (layer is null || layer.LayerGUID.Equals(string.Empty) || ClipImage.ContainsKey(layer.LayerGUID)) continue;
                Rectangle rectangle = ProcessRectangle(layer.Srcquad);
                ClipImage[layer.LayerGUID] = CutImage(image,rectangle);
            }
        }
        //CombineCutImage(quad);
    }
    public Dictionary<int, Image> KeyframeImage = new();
    void CombineCutImage(QuadJson quad)
    {
        var image = new Image<Rgba32>((int)quad.Keyframe[0].Width, (int)quad.Keyframe[0].Height);
        image.Mutate(x =>
        {
            foreach (var layer in quad.Keyframe[0].Layers)
            {
                x.DrawImage(ClipImage[layer.LayerGUID],
                    new Point((int)(layer.Dstquad[0] + quad.Keyframe[1].Width / 2), (int)(layer.Dstquad[1] + quad.Keyframe[1].Height / 2)),
                    1);
                //Console.WriteLine((int)(layer.Dstquad[0]+quad.Keyframe[1].Width/2)+":::"+ (int)(layer.Dstquad[1] + quad.Keyframe[1].Height / 2));
            }
        });
        image.SaveAsPng("D:\\Download\\quad_mobile_v05_beta-20240404-2000\\quad_mobile_v05_beta\\1.png");
    }
    private Rectangle ProcessRectangle(float[] srcquad)
    {
        var points = ProcessTools.FindMinAndMaxPoints(srcquad);
        return new Rectangle()
        {
            X = (int)points[0],
            Y = (int)points[1],
            Width = (int)(points[2] - points[0]),
            Height = (int)(points[3] - points[1]),
        };
    }
    private Image ReadImage(string src)
    {
        return Image.Load(src);
    }
    private Image CutImage(Image image,Rectangle rectangle)
    {
        var newImage = image.Clone(context =>
        {
            context.Crop(rectangle);
        });
        return newImage;
    }
}