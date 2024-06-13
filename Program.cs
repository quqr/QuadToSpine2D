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
        List<List<string>> imagePath =
        [
            // [
            //     "E:\\Asset\\momohime\\4k\\00Files\\file\\Momohime_Katana.0.tpl.png",
            //     "E:\\Asset\\momohime\\4k\\00Files\\file\\Momohime_Katana.1.tpl.png",
            //     "E:\\Asset\\momohime\\4k\\00Files\\file\\Momohime_Katana.2.tpl.png",
            //     "E:\\Asset\\momohime\\4k\\00Files\\file\\Momohime_Katana.3.tpl.png",
            //     "E:\\Asset\\momohime\\4k\\00Files\\file\\Momohime_Katana.4.tpl.png",
            //     "E:\\Asset\\momohime\\4k\\00Files\\file\\Momohime_Katana.5.tpl.png",
            // ],
            // [                
            //     "E:\\Asset\\momohime\\4k\\00Files\\file\\Momohime_Katana_Dark_tex.0.tpl.png",
            //     "E:\\Asset\\momohime\\4k\\00Files\\file\\Momohime_Katana_Dark_tex.1.tpl.png",
            //     "E:\\Asset\\momohime\\4k\\00Files\\file\\Momohime_Katana_Dark_tex.2.tpl.png",
            //     "E:\\Asset\\momohime\\4k\\00Files\\file\\Momohime_Katana_Dark_tex.3.tpl.png",
            //     "E:\\Asset\\momohime\\4k\\00Files\\file\\Momohime_Katana_Dark_tex.4.tpl.png",
            //     "E:\\Asset\\momohime\\4k\\00Files\\file\\Momohime_Katana_Dark_tex.5.tpl.png",
            // ],
            // [
            //     "D:\\Download\\quad_mobile_v05_beta-20240404-2000\\quad_mobile_v05_beta\\data\\swi sent Fuyusaka00.0.nvt.png",
            //     "D:\\Download\\quad_mobile_v05_beta-20240404-2000\\quad_mobile_v05_beta\\data\\swi sent Fuyusaka00.1.nvt.png"
            // ]
            [
                "D:\\Download\\quad_mobile_v05_beta-20240404-2000\\quad_mobile_v05_beta\\data\\swi unic BlackKnight_HG_M00.0.nvt.png",
                "D:\\Download\\quad_mobile_v05_beta-20240404-2000\\quad_mobile_v05_beta\\data\\swi unic BlackKnight_HG_M00.1.nvt.png"
            
            ]
        ];
        const string jsonOutputPath = @"E:\Asset\ttt\";
        const string imageSavePath = @"E:\Asset\ttt\images";
        var quadPath =
            //@"E:\Asset\momohime\4k\00Files\file\Momohime_Katana_a.mbs.v55.quad";
            //@"E:\Asset\momohime\4k\00Files\file\Momohime_Battle.mbs.v55.quad";
            
            //@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.mbs.v55.quad";
            
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi unic BlackKnight_HG_M.mbs.v55.quad";
            //@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Amiguchi00.mbs.v55.quad";
        const int scaleFactor = 1;
        if (!Directory.Exists(imageSavePath)) Directory.CreateDirectory(imageSavePath);
        ProcessJson(quadPath, imagePath ,imageSavePath, jsonOutputPath,scaleFactor);
    }

    private static void ReleaseMode()
    {
        List<List<string>> imagePath = [];
        var jsonOutputPath = $"{Directory.GetCurrentDirectory()}/";
        var imageSavePath = $"{Directory.GetCurrentDirectory()}/images";
        var scaleFactor = 1;
        var quadPath = LoadPaths(imageSavePath, imagePath, ref scaleFactor);
        ProcessJson(quadPath, imagePath, imageSavePath, jsonOutputPath,scaleFactor);
        Console.ReadLine();
    }

    private static string LoadPaths(string imageSavePath, List<List<string>> imagePath, ref int scaleFactor)
    {
        if (!Directory.Exists(imageSavePath)) Directory.CreateDirectory(imageSavePath);
        var quadPath = string.Empty;
        Console.WriteLine(">>> Please input *.quad path");
        while (true)
        {
            quadPath = Console.ReadLine();
            if (quadPath.Split('.').Last().Contains("quad"))
            {
                quadPath = quadPath.Replace("'", "");
                break;
            }
            Console.WriteLine(">>> Not .quad file, try again!");
        }

        Console.WriteLine(">>> Please input images path, input ok to exit");

        int curSkin = 0;
        imagePath.Add([]);
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

            if (path.ToLower().Equals("s"))
            {
                imagePath.Add([]);
            }

            if (path.Split('.').Last().Contains("png"))
            {
                path = path.Replace("'", "");
                imagePath[curSkin].Add(path);
            }
            Console.WriteLine(">>> Please input images path, input ok to exit, input s to input next skin path");
            Console.Write(">>>");
        }
        Console.WriteLine(">>> Please input scale factor (default 1)");
        var scale = Console.ReadLine();
        scaleFactor = Convert.ToInt32(scale == "" ? 1 : scale);
        
        return quadPath;
    }

    private static void ProcessJson(string quadPath, List<List<string>> imageSrc,string imageSavePath,
        string jsonOutputPath,int scaleFactor)
    {
        var quad = new ProcessQuadFile();
        var imageQuad = new ProcessImage();
        var spineJson = new ProcessSpineJson();

        quad.Load(quadPath, scaleFactor);
        imageQuad.Process(imageSrc ,quad.Quad, imageSavePath);
        spineJson.Process(imageQuad, quad.Quad, jsonOutputPath);
        
        Console.WriteLine("Process Finish...");
    }
}