namespace QuadPlayer;

public class ProcessQuadFile
{
    public QuadJson Quad;
    public ProcessQuadFile(string quadPath)
    {
        var json = File.ReadAllText(quadPath);
        Quad = JsonConvert.DeserializeObject<QuadJson>(json);
        Quad.Keyframe.RemoveAll(x => x.Layer[0] is null);
        for (var i = 0; i < Quad.Keyframe.Count; i++)
        {
            //if (Quad.Keyframe[i] is null)continue;
            var points = new[]
            {
                float.MaxValue,float.MaxValue,float.MinValue,float.MinValue,
            };
            for (var j = 0; j < Quad.Keyframe[i].Layer.Count; j++)
            {
                //if (Quad.Keyframe[i].Layer[j] is null) continue;
                Quad.Keyframe[i].Layer[j].CalculateRotate();
                points = ProcessTools.FindMinAndMaxPoints(points, Quad.Keyframe[i].Layer[j].MinAndMaxDstPoints);
            }
            Quad.Keyframe[i].CalculateRect(points);
        }
        Console.WriteLine("ProcessQuadFile Finished");
    }
}