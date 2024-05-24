using QuadPlayer.JsonConverters;

namespace QuadPlayer.Spine;
public class SpineJson
{
    public Skeleton Skeletons{ get; set; } = new();
    public List<Bone> Bones{ get; set; } = [];
    public List<Slot> Slots{ get; set; }= [];
    public List<Skin> Skins{ get; set; } = new(1);
    public Dictionary<string,SpineAnimation> Animations { get; set; } = new();
}

public class Skeleton
{
    public string Spine="3.8";
    public string ImagesPath="";
}

public class Slot
{
    public string Name { get; set; }
    public string Bone { get; set; }
    [JsonIgnore]
    public string Attachment { get; set; }
    [JsonIgnore]
    public int Order { get; set; }
}

public class Skin
{
    public string Name = "default";

    [JsonConverter(typeof(AttachmentsJsonConverter<Attachments>))]
    public List<Attachments> Attachments = [];
}
public class Attachments
{
    public Mesh Value{ get; set; }
}
public class Mesh
{
    [JsonIgnore]
    public string Name;

    public string Type{ get; set; } = "mesh";
    public float[] Uvs{ get; set; } = new float[8];
    public float[] Triangles{ get; set; } = [1, 2, 3, 1, 3, 0];
    public float[] Vertices{ get; set; } = new float[8];
    public int Hull{ get; set; } = 4;
}
public class Bone
{
    public string Name{ get; set; }
}
public class SpineAnimation
{
    public Dictionary<string,AnimationSlot> Slots{ get; set; } = new();
    public Deform Deform{ get; set; } = new();
    public List<DrawOrder> DrawOrder{ get; set; } = [];
}

public class DrawOrder
{
    public float Time { get; set; }
    public List<DrawOrderOffset> Offsets{ get; set; } = [];
}

public class DrawOrderOffset
{
    [JsonIgnore]public int SlotNum { get; set; } = 0;
    public string Slot { get; set; } = string.Empty;
    public int Offset { get; set; }
}
public class AnimationSlot
{
    public List<AnimationAttachment> Attachment = [];
}

public class AnimationAttachment
{
    public float Time { get; set; }
    public string? Name { get; set; }
}

public class Deform
{
    public Dictionary<string,AnimationDefault> @Default = new();
}
[JsonConverter(typeof(AnimationDefaultJsonConverter))]
public class AnimationDefault
{
    public string Name { get; set; }
    public List<AnimationVertices> ImageVertices { get; set; } = [];
}
public class AnimationVertices
{
    public float Time { get; set; }
    public float[] Vertices { get; set; } = new float[8];
}


