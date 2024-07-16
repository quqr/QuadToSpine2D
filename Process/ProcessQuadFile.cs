using QuadToSpine.Data.Quad;

namespace QuadToSpine.Process;

public class ProcessQuadFile
{
    public QuadJson QuadData { get; private set; }

    public void Load(string quadPath)
    {
        Console.WriteLine("Loading quad file...");
        GlobalData.LabelContent = "Loading quad file...";
        var json = File.ReadAllText(quadPath);
        QuadData = JsonConvert.DeserializeObject<QuadJson>(json)!;
        QuadData.Skeleton.RemoveAll(x => x is null);
        QuadData.Animation.RemoveAll(x => x is null || x.ID == -1);
        foreach (var keyframe in QuadData.Keyframe)
            keyframe?.Layer?.RemoveAll(y => y is null || y.LayerGuid.Equals(string.Empty));
        QuadData.Keyframe.RemoveAll(x => x?.Layer is null || x.Layer.Count == 0);
        GlobalData.LabelContent = "Quad file loaded";
        Console.WriteLine("Quad file loaded");
    }
}