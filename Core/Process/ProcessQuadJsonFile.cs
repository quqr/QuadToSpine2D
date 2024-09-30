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
        GlobalData.BarValue       = 0;

        var json = File.ReadAllText(quadPath);
        GlobalData.BarValue = 5;
        QuadData            = JsonConvert.DeserializeObject<QuadJsonData>(json);
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
        Parallel.ForEach(QuadData.Skeleton, skeleton =>
        {
            var animations = new List<Animation>();
            animations
               .AddRange(skeleton.Bone
                                 .Select(bone => QuadData.Animation
                                                         .First(x => x.Id == bone.Attach.Id)));
            skeleton.CombineAnimation = ProcessUtility.CombineAnimations(animations);
        });
    }

    private void RemoveAllNull()
    {
        QuadData.Skeleton.RemoveAll(x => x is null);
        QuadData.Animation.RemoveAll(x => x is null || x.Id == -1);

        foreach (var keyframe in QuadData.Keyframe) keyframe?.Layers?.RemoveAll(y => y is null);
        QuadData.Keyframe.RemoveAll(x => x?.Layers is null || x.Layers.Count == 0);
        Parallel.ForEach(QuadData.Animation, animation =>
        {
            foreach (var timeline in animation.Timeline)
                switch (timeline.Attach?.AttachType)
                {
                    case AttachType.Keyframe:
                        timeline.Attach.Keyframe =
                            QuadData.Keyframe
                                    .Find(x => x.Id == timeline.Attach.Id);
                        break;
                    case AttachType.Slot:
                        var attach = QuadData.Slot[timeline.Attach.Id].Attaches
                                            ?.Find(x => x.AttachType == AttachType.Keyframe);
                        timeline.Attach.Keyframe =
                            QuadData.Keyframe.Find(x => x.Id == attach?.Id);
                        break;
                    case AttachType.HitBox:
                        timeline.Attach.Hitbox =
                            QuadData.Hitbox[timeline.Attach.Id];
                        break;
                }
        });
        QuadData.Hitbox.RemoveAll(x => x is null);

        // Attributes = QuadData.Keyframe
        //     .SelectMany(x => x.Layer.Where(y => y?.Attribute is not null))
        //     .ToDictionary(z=>z.Attribute);
    }
}