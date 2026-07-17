using VanillawareConverter.Mbs.Parsers;
using VanillawareConverter.Mbs.Models;

var parser = new MbsToV55Parser();
var files = Directory.GetFiles(@"F:\Codes\13\13rim ex", "*.mbs", SearchOption.AllDirectories);
var failCount = 0;

foreach (var f in files.Take(5))
{
    var name = Path.GetFileNameWithoutExtension(f);
    try
    {
        var data = File.ReadAllBytes(f);
        var tag = PlatformConfigs.DetectPlatform(data);
        if (tag == PlatformTag.Unknown)
        {
            Console.WriteLine($"{name}: Unknown platform (ver=0x{BitConverter.ToUInt16(data, 0x14):X4})");
            continue;
        }
        var result = parser.Parse(data, tag);
        Console.WriteLine($"{name}: OK (tag={tag}, sections parsed)");
    }
    catch (Exception ex)
    {
        failCount++;
        Console.WriteLine($"{name}: FAILED");
        Console.WriteLine($"  Type: {ex.GetType().FullName}");
        Console.WriteLine($"  Message: {ex.Message}");
        Console.WriteLine($"  StackTrace:\n{ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"  Inner: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
            Console.WriteLine($"  Inner Stack: {ex.InnerException.StackTrace}");
        }
        Console.WriteLine();
    }
}

Console.WriteLine($"\nFailures in first 5: {failCount}");
