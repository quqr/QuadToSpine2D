using System.Threading.Tasks;
using QuadToSpine2D.Core.Utility;

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
        
        GlobalData.BarValue = 15;
        
        RemoveAllNull();
        CombineAnimations();
        
        GlobalData.BarTextContent = "Quad file loaded";
        Console.WriteLine("Quad file loaded");
        GlobalData.BarValue = 30;
        return QuadData;
    }

    private void CombineAnimations()
    {
        foreach (var skeleton in QuadData.Skeleton)
        {
            var animations = new List<Animation>();
            animations
                .AddRange(skeleton.Bone
                    .Select(bone => QuadData.Animation
                        .First(x => x.Id == bone.Attach.Id)));
            skeleton.CombineAnimation = ProcessUtility.CombineAnimations(animations);
        }
    }

    private void RemoveAllNull( )
    {
        QuadData.Skeleton.RemoveAll(x => x is null);
        QuadData.Animation.RemoveAll(x => x is null || x.Id == -1);

        foreach (var keyframe in QuadData.Keyframe)
        {
            keyframe?.Layer?.RemoveAll(y => y is null ||
                                            y.LayerGuid.Equals(string.Empty) ||
                                            y.TexId == -1 ||
                                            y.BlendId != 0);
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
        Parallel.ForEach(QuadData.Animation, animation =>
        {
            foreach (var timeline in animation.Timeline)
            {
                switch (timeline.Attach?.AttachType)
                {
                    case AttachType.Keyframe:
                        timeline.Attach.Keyframe = QuadData.Keyframe.Find(x => x.Id == timeline.Attach.Id);
                        break;
                    case AttachType.Slot:
                        var attach = QuadData.Slot[timeline.Attach.Id].Attaches
                            ?.Find(x => x.AttachType == AttachType.Keyframe);
                        timeline.Attach.Keyframe = QuadData.Keyframe.Find(x => x.Id == attach?.Id);
                        break;
                    case AttachType.HitBox:
                        timeline.Attach.Hitbox = QuadData.Hitbox[timeline.Attach.Id];
                        break;
                }
            }
        });
        QuadData.Hitbox.RemoveAll(x => x is null);
        
        // Attributes = QuadData.Keyframe
        //     .SelectMany(x => x.Layer.Where(y => y?.Attribute is not null))
        //     .ToDictionary(z=>z.Attribute);
    }
}