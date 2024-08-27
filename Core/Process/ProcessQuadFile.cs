namespace QuadToSpine2D.Core.Process;

public class ProcessQuadFile
{
    private QuadJson QuadData { get; set; }
    // private Dictionary<List<string>, KeyframeLayer> Attributes { get; set; } = [];
    // private Dictionary<List<string>, KeyframeLayer> Fogs { get; set; } = [];

    public QuadJson LoadQuadJson(string quadPath)
    {
        Console.WriteLine("Loading quad file...");
        GlobalData.LabelContent = "Loading quad file...";

        var json = File.ReadAllText(quadPath);
        QuadData = JsonConvert.DeserializeObject<QuadJson>(json)!;

        QuadData.Skeleton.RemoveAll(x => x is null);
        QuadData.Animation.RemoveAll(x => x is null || x.Id == -1);


        foreach (var keyframe in QuadData.Keyframe)
        {
            keyframe?.Layer?.RemoveAll(y => y is null ||
                                            y.LayerGuid.Equals(string.Empty) ||
                                            y.TexId == -1 ||
                                            y.BlendId != 0);
            //keyframe?.Layer?.RemoveAll(x => x is null);
        }
        
        QuadData.Keyframe.RemoveAll(x => x?.Layer is null || x.Layer.Count == 0);
        
        // Attributes = QuadData.Keyframe
        //     .SelectMany(x => x.Layer.Where(y => y?.Attribute is not null))
        //     .ToDictionary(z=>z.Attribute);
        
        GlobalData.LabelContent = "Quad file loaded";
        Console.WriteLine("Quad file loaded");
        return QuadData;
    }
}