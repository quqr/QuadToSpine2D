using QTSAvalonia.Helper;
using QTSCore.Data.Quad;
using QTSCore.JsonConverters;
using QTSCore.Utility;

namespace QTSCore.Process;

public class ProcessQuadJsonFile
{
    private QuadJsonData QuadData { get; set; }

    public QuadJsonData LoadQuadJson(string quadPath, bool isPostProcess = false)
    {
        LoggerHelper.Info($"Loading quad file {quadPath}");
        ClearConverter();
        var json = File.ReadAllText(quadPath);

        // 添加详细的反序列化调试信息
        var settings = new JsonSerializerSettings
        {
            Error = (sender, args) =>
            {
                LoggerHelper.Error($"JSON deserialization error: {args.ErrorContext.Error.Message}");
                args.ErrorContext.Handled = true;
            },
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        QuadData = JsonConvert.DeserializeObject<QuadJsonData>(json, settings) ??
                   throw new ArgumentException("Invalid quad file");
        if (isPostProcess)
        {
            LoggerHelper.Info("Combining animations...");
            InitializeData();
            CombineAnimations();
            LoggerHelper.Info("Combining animations completed");
        }

 

        LoggerHelper.Info("Quad file loaded successfully");
        return QuadData;
    }

    private void ClearConverter()
    {
        HitboxJsonConverter.IdCount = -1;
        SlotJsonConverter.IdCount = -1;
        SkeletonJsonConverter.IdCount = -1;
        KeyframeJsonConverter.IdCount = -1;
    }

    private void CombineAnimations()
    {
        foreach (var skeleton in QuadData.Skeleton)
        {
            if (skeleton?.Bone is null) continue;
            var animations = new List<Animation?>();
            animations
                .AddRange(skeleton.Bone
                    .Select(bone => QuadData.Animation
                        .First(x =>
                        {
                            if (x is null) return false;
                            return x.Id == bone.Attach.Id;
                        })));
            skeleton.CombineAnimation = ProcessUtility.CombineAnimations(animations);
        }
    }

    private void InitializeData()
    {
        Parallel.ForEach(QuadData.Animation, SetAttaches);
    }

    private void SetAttaches(Animation? animation)
    {
        if (animation is null) return;
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
                for (var index = 0; index < QuadData.Slot[targetId].Attaches.Length; index++)
                    QuadData.Slot[targetId].Attaches[index] = GetAttach(
                        QuadData.Slot[targetId].Attaches[index].AttachType,
                        QuadData.Slot[targetId].Attaches[index].Id)!;
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