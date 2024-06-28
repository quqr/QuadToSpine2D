namespace QuadToSpine.Process;

public class ProcessQuadFile
{
    public QuadJson Quad{ get; set; }

    public void Load(string quadPath, int scaleFactor)
    {
        Console.WriteLine("Loading quad file...");

        TransmissionData.Instance.ScaleFactor = scaleFactor > 1 ? scaleFactor : 1;
        var json = File.ReadAllText(quadPath);
        Quad = JsonConvert.DeserializeObject<QuadJson>(json)!;
        Quad.Skeleton.RemoveAll(x => x is null);
        Quad.Animation.RemoveAll(x => x is null || x.ID == -1);
        foreach (var keyframe in Quad.Keyframe)
            keyframe?.Layer?.RemoveAll(y => y is null || y.LayerGuid.Equals(string.Empty));
        Quad.Keyframe.RemoveAll(x => x?.Layer is null || x.Layer.Count == 0);
        Console.WriteLine("Quad file loaded");
    }
}