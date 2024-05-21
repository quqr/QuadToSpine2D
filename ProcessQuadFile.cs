namespace QuadPlayer;

public class ProcessQuadFile
{
    public QuadJson Quad;
    public ProcessQuadFile(string quadPath)
    {
        var json = File.ReadAllText(quadPath);
        Quad = JsonConvert.DeserializeObject<QuadJson>(json);
        Quad.Keyframe.RemoveAll(x => x.Layer[0] is null);
        Quad.Keyframe
            .ForEach(x => x.Layer.
                RemoveAll(y => y.LayerGuid.Equals(string.Empty)));
        Quad.Skeleton.RemoveAll(x => x is null);
        Quad.Animation.RemoveAll(x => x is null);
    }
}