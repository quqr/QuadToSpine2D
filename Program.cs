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
            @"E:\Asset\momohime\4k\00Files\file\Momohime.0.tpl1.png",
            @"E:\Asset\momohime\4k\00Files\file\Momohime.1.tpl.png",
            @"E:\Asset\momohime\4k\00Files\file\Momohime.2.tpl.png"
        ];
        var jsonOutputPath = "E:\\Asset\\ttt\\result.json";
        var imageSavePath = "E:\\Asset\\ttt\\images";
        var quadPath =
            @"E:\Asset\momohime\4k\00Files\file\Momohime_Battle.mbs.v55.quad";
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