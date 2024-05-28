using QuadPlayer;

internal class Program
{
    private static void Main(string[] args)
    {
        ReleaseMode();
        //DebugMode();
    }

    private static void DebugMode()
    {
        List<string> imagePath =
        [
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi unic BlackKnight_HG_M00.0.nvt.png",
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi unic BlackKnight_HG_M00.1.nvt.png",
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.2.gnf.png"

        ];
        var jsonOutputPath = "E:\\Asset\\ttt\\result.json";
        var imageSavePath = "E:\\Asset\\ttt\\images";
        var quadPath =
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi unic BlackKnight_HG_M.mbs.v55.quad";
        int scaleFactor = 1;
        ProcessJson(quadPath, imagePath, imageSavePath, jsonOutputPath,scaleFactor);
    }

    private static void ReleaseMode()
    {
        List<string> imagePath = [];
        var jsonOutputPath = $"{Directory.GetCurrentDirectory()}/result.json";
        var imageSavePath = $"{Directory.GetCurrentDirectory()}/images";
        int scaleFactor = 1;
        var quadPath = LoadPaths(imageSavePath, imagePath, ref scaleFactor);
        ProcessJson(quadPath, imagePath, imageSavePath, jsonOutputPath,scaleFactor);
        Console.ReadLine();
    }

    private static string LoadPaths(string imageSavePath, List<string> imagePath, ref int scaleFactor)
    {
        if (!Directory.Exists(imageSavePath)) Directory.CreateDirectory(imageSavePath);
        var quadPath = string.Empty;
        Console.WriteLine(">>> Please input *.quad path");
        while (true)
        {
            quadPath = Console.ReadLine();
            if (quadPath.Split('.').Last().Equals("quad")) break;
            Console.WriteLine(">>> Not .quad file, try again!");
        }

        Console.WriteLine(">>> Please input images path, input ok to exit");
        while (true)
        {
            var path = Console.ReadLine();
            if (path.ToLower().Equals("ok"))
            {
                if (imagePath.FirstOrDefault() is null)
                    Console.WriteLine(">>> There is no image, try again");
                else
                    break;
            }
            if (path.Split('.').Last().Equals("png")) imagePath.Add(path);
            Console.WriteLine(">>> Please input images path, input ok to exit");
            Console.Write(">>>");
        }
        Console.WriteLine(">>> Please input scale factor (default 1)");
        var value = Console.ReadLine();
        scaleFactor = Convert.ToInt32(value);
        
        return quadPath;
    }

    private static void ProcessJson(string quadPath, List<string> imagePath, string imageSavePath,
        string jsonOutputPath,int scaleFactor)
    {
        var quad = new ProcessQuadFile(quadPath,scaleFactor);
        var imageQuad = new ProcessImage(imagePath, quad.Quad, imageSavePath);
        var spineJson = new ProcessSpineJson(imageQuad, quad.Quad);
        File.WriteAllText(jsonOutputPath, spineJson.SpineJsonFile);
        Console.WriteLine("Process Finished...");
    }
}