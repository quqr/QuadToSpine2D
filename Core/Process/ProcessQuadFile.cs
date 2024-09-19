using System.Threading.Tasks;

namespace QuadToSpine2D.Core.Process;

public class ProcessQuadFile
{
    private QuadJson? QuadData { get; set; }
    // private Dictionary<List<string>, KeyframeLayer> Attributes { get; set; } = [];
    // private Dictionary<List<string>, KeyframeLayer> Fogs { get; set; } = [];

    public QuadJson? LoadQuadJson(string quadPath)
    {
        Console.WriteLine("Loading quad file...");
        GlobalData.BarTextContent = "Loading quad file...";
        GlobalData.BarValue = 0;

        var json = File.ReadAllText(quadPath);
        GlobalData.BarValue = 5;

        QuadData = JsonConvert.DeserializeObject<QuadJson>(json);

        GlobalData.BarValue = 15;

        if (QuadData is null) return null;
        
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
        Parallel.ForEach(QuadData.Keyframe, keyframe =>
        {
            for (var i = 0; i < keyframe.Layer.Count; i++)
            {
                keyframe.Layer[i].LastLayer = i == 0 ? null : keyframe.Layer[i - 1];
                keyframe.Layer[i].NextLayer = i == keyframe.Layer.Count - 1 ? null : keyframe.Layer[i + 1];
            }
        });
        // Attributes = QuadData.Keyframe
        //     .SelectMany(x => x.Layer.Where(y => y?.Attribute is not null))
        //     .ToDictionary(z=>z.Attribute);
        
        GlobalData.BarTextContent = "Quad file loaded";
        Console.WriteLine("Quad file loaded");
        GlobalData.BarValue = 30;
        return QuadData;
    }
}