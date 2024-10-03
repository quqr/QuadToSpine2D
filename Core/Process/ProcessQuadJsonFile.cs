﻿using System.Threading.Tasks;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core.Process;

public class ProcessQuadJsonFile
{
    private QuadJsonData QuadData { get; set; }

    public QuadJsonData? LoadQuadJson(string quadPath)
    {
        Console.WriteLine("Loading quad file...");
        GlobalData.BarTextContent = "Loading quad file...";
        GlobalData.BarValue       = 0;

        var json = File.ReadAllText(quadPath);
        GlobalData.BarValue = 5;
        
        QuadData = JsonConvert.DeserializeObject<QuadJsonData>(json) ??
                   throw new ArgumentException("Invalid quad file");

        GlobalData.BarValue = 15;

        InitData();
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

    private void InitData()
    {
        Parallel.ForEach(QuadData.Animation, SetAttaches);
        
        QuadData.Skeleton.RemoveAll(x => x is null);
        QuadData.Animation.RemoveAll(x => x is null || x.Id == -1);
        
        foreach (var keyframe in QuadData.Keyframe) keyframe?.Layers?.RemoveAll(y => y is null);
        
        QuadData.Keyframe.RemoveAll(x => x?.Layers is null || x.Layers.Count == 0);
        
        QuadData.Hitbox.RemoveAll(x => x is null);

        // Attributes = QuadData.Keyframe
        //     .SelectMany(x => x.Layer.Where(y => y?.Attribute is not null))
        //     .ToDictionary(z=>z.Attribute);
    }

    private void SetAttaches(Animation? animation)
    {
        if(animation is null) return;
        foreach (var timeline in animation.Timeline)
        {
            if (timeline.Attach is null) continue;
            timeline.Attach = GetAttach(timeline.Attach.AttachType, timeline.Attach.Id);
        }
    }
    private Attach? GetAttach(AttachType attachType, int targetId)
    {
        switch (attachType)
        {
            case AttachType.Keyframe:
                return QuadData.Keyframe[targetId];
            case AttachType.Slot: 
                return QuadData.Slot[targetId];
            case AttachType.HitBox:
                return QuadData.Hitbox[targetId];
            case AttachType.Animation:
                return QuadData.Animation[targetId];
            case AttachType.Skeleton:
                return QuadData.Skeleton[targetId];
            default:
                throw new ArgumentOutOfRangeException(nameof(attachType), attachType, null);
        }
    }
}