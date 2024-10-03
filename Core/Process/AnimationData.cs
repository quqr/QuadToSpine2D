namespace QuadToSpine2D.Core.Process;

public class AnimationData
{
    public string                        Name   { get; set; } = string.Empty;
    public bool                          IsLoop { get; set; }
    public bool                          IsMix  { get; set; }
    public Dictionary<int, Attachment> Data   { get; set; } = [];
}

public class Attachment
{
    public List<Timeline> DisplayAttachments { get; set; } = [];
    public List<Timeline> ConcealAttachments { get; set; } = [];
}