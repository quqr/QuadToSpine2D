using QuadPlayer;
using QuadPlayer.Spine;

class Program
{
    static void Main(string[] args)
    {
        var quadPath = "D:\\Download\\quad_mobile_v05_beta-20240404-2000\\quad_mobile_v05_beta\\data\\swi sent Amiguchi00.mbs.v55.quad";
        var imagePath = "D:\\Download\\quad_mobile_v05_beta-20240404-2000\\quad_mobile_v05_beta\\data\\swi sent Amiguchi00.0.nvt.png";
        var outputPath = "E:\\Asset\\Muramasa[PICTURE]\\WORK\\TestSpine\\test.json";
        var quad = new ProcessQuadFile(quadPath);
        var imageQuad = new ProcessImage(imagePath, quad.Quad);
        var spineJson = new ProcessSpineJson(imageQuad,quad.Quad);
        File.WriteAllText(outputPath,spineJson.SpineJsonFile);
    }
}