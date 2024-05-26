namespace QuadPlayer;

public class ProcessQuadFile
{
    public QuadJson Quad;

    public ProcessQuadFile(string quadPath)
    {
        Console.WriteLine("Loading quad file...");
        var json = File.ReadAllText(quadPath);
        Quad = JsonConvert.DeserializeObject<QuadJson>(json);
        Quad.Keyframe.RemoveAll(x => x?.Layer is null || x.Layer.Count == 0);
        Quad.Keyframe
            .ForEach(x =>
            {
                x.Layer.RemoveAll(y => y is null || y.LayerGuid.Equals(string.Empty));
                for (var i = 0; i < x.Layer.Count; i++) x.Layer[i].Order = i;
            });
        Quad.Skeleton.RemoveAll(x => x is null);
        Quad.Animation.RemoveAll(x => x is null);
        Console.WriteLine("Quad file loaded");
    }
}