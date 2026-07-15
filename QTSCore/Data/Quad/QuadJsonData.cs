namespace QTSCore.Data.Quad;

public class QuadJsonData
{
    public Keyframe?[] Keyframe { get; set; } = [];
    public Animation?[] Animation { get; set; } = [];
    public QuadSkeleton?[] Skeleton { get; set; } = [];
    public Slot[] Slot { get; set; } = [];
    public Hitbox?[] Hitbox { get; set; } = [];
    public Blend[] Blend { get; set; } = [];
    public string[] Mix { get; set; } = [];
    public Link[] Link { get; set; } = [];

}
