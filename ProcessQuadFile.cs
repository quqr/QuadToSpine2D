﻿namespace QuadPlayer;

public class ProcessQuadFile
{
    public QuadJson Quad;
    public ProcessQuadFile(string quadPath)
    {
        Console.WriteLine("Loading quad file...");
        var json = File.ReadAllText(quadPath);
        Quad = JsonConvert.DeserializeObject<QuadJson>(json);
        Quad.Keyframe.RemoveAll(x => x.Layer[0] is null);
        Quad.Keyframe
            .ForEach(x =>
            {
                x.Layer.RemoveAll(y => y.LayerGuid.Equals(string.Empty));
                for (int i = 0; i < x.Layer.Count; i++)
                {
                    x.Layer[i].Order = x.Layer.Count - i;
                }
            });
        Quad.Skeleton.RemoveAll(x => x is null);
        Quad.Animation.RemoveAll(x => x is null);
        Console.WriteLine("Quad file loaded");
    }
}