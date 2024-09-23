namespace QuadToSpine2D.Core.Process;

public class ProcessQuadJsonFile
{
    private QuadJsonData? QuadData { get; set; }
    
    public QuadJsonData? LoadQuadJson(string quadPath)
    {
        Console.WriteLine("Loading quad file...");
        GlobalData.BarTextContent = "Loading quad file...";
        GlobalData.BarValue = 0;

        var json = File.ReadAllText(quadPath);
        GlobalData.BarValue = 5;
        QuadData = JsonConvert.DeserializeObject<QuadJsonData>(json);
        if (QuadData is null) return null;
        
        QuadData.RemoveAllNull();
        
        GlobalData.BarValue = 15;
        GlobalData.BarTextContent = "Quad file loaded";
        Console.WriteLine("Quad file loaded");
        GlobalData.BarValue = 30;
        return QuadData;
    }
    
}