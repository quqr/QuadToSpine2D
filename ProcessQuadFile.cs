namespace QuadPlayer;

public class ProcessQuadFile
{
    public QuadJson Quad;
    public ProcessQuadFile(string quadPath,int scaleFactor)
    {
        Console.WriteLine("Loading quad file...");
        
        TransmissionData.Instance.ScaleFactor = scaleFactor > 1 ? scaleFactor : 1;
        var json = File.ReadAllText(quadPath);
        Quad = JsonConvert.DeserializeObject<QuadJson>(json);
        Quad.Keyframe.RemoveAll(x => x?.Layer is null || x.Layer.Count == 0);
        Quad.Skeleton.RemoveAll(x => x is null);
        Quad.Animation.RemoveAll(x => x is null);
        foreach (var keyframe in Quad.Keyframe)
        {
            keyframe.Layer.RemoveAll(y => y is null || y.LayerGuid.Equals(string.Empty));
        }
        Console.WriteLine("Quad file loaded");
    }
}