using QTSAvalonia.Helper;

namespace QTSCore.Data.Quad;

public class Attach
{
    public Attach()
    {
    }

    public Attach(AttachType attachType, int id)
    {
        AttachType = attachType;
        Id = id;
    }

    [JsonProperty]
    private string Type
    {
        set
        {
            switch (value)
            {
                case "keyframe":
                    AttachType = AttachType.Keyframe;
                    break;
                case "slot":
                    AttachType = AttachType.Slot;
                    break;
                case "animation":
                    AttachType = AttachType.Animation;
                    break;
                case "skeleton":
                    AttachType = AttachType.Skeleton;
                    break;
                case "hitbox":
                    AttachType = AttachType.HitBox;
                    break;
                default:
                    LoggerHelper.Info($"Can not process attach type : {value}");
                    break;
            }
        }
    }

    public AttachType AttachType { get; set; }
    public int Id { get; set; } = -1;
}