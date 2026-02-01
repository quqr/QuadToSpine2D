using QTSCore.Data.Quad;

namespace QTSCore.Data;

public class AnimationData
{
    public bool IsLoop { get; set; }

    public bool IsMix { get; set; }

    // time:{attachment}
    public Dictionary<int, Attachment> Data { get; set; } = [];
}

public class Attachment
{
    public List<Timeline> DisplayAttachments { get; } = [];
    public List<Timeline> ConcealAttachments { get; } = [];
}