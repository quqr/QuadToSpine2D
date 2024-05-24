using QuadPlayer;

class Program
{
    static void Main(string[] args)
    {
        ReleaseMode();
        //DebugMode();
    }

    private static void DebugMode()
    {
        List<string> imagePath =
        [
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.0.nvt.png",
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.1.nvt.png"
        ];
        var jsonOutputPath = $"{Directory.GetCurrentDirectory()}/result.json";
        var imageSavePath = $"{Directory.GetCurrentDirectory()}/images";
        var quadPath =
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.mbs.v55.quad";
        ProcessJson(quadPath, imagePath, imageSavePath, jsonOutputPath);
    }

    private static void ReleaseMode()
    {
        List<string> imagePath = [];
        var jsonOutputPath = $"{Directory.GetCurrentDirectory()}/result.json";
        var imageSavePath = $"{Directory.GetCurrentDirectory()}/images";
        var quadPath = LoadPaths(imageSavePath, imagePath);
        ProcessJson(quadPath, imagePath, imageSavePath, jsonOutputPath);
        Console.ReadLine();
    }

    private static string LoadPaths(string imageSavePath, List<string> imagePath)
    {
        if (!Directory.Exists(imageSavePath))
        {
            Directory.CreateDirectory(imageSavePath);
        }
        var quadPath = string.Empty;
        Console.WriteLine("Please input *.quad path");
        while (true)
        {
            quadPath = Console.ReadLine();
            if(quadPath.Split('.').Last().Equals("quad")) break;
            Console.WriteLine("Not .quad file, try again!");
        }
        Console.WriteLine("Please input images path, input ok exit");
        while (true)
        {
            var path = Console.ReadLine();
            if (path.ToLower().Equals("ok"))
            {
                if(imagePath.FirstOrDefault() is null)
                    Console.WriteLine("There is no image, try again");
                else
                    break;
            }
            if (path.Split('.').Last().Equals("png")) imagePath.Add(path);
            Console.WriteLine("ok!");
        }

        return quadPath;
    }

    private static void ProcessJson(string quadPath, List<string> imagePath, string imageSavePath, string jsonOutputPath)
    {
        var quad = new ProcessQuadFile(quadPath);
        var imageQuad = new ProcessImage(imagePath, quad.Quad,imageSavePath);
        var spineJson = new ProcessSpineJson(imageQuad,quad.Quad);
        File.WriteAllText(jsonOutputPath,spineJson.SpineJsonFile);
    }
}