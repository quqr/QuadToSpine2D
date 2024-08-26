﻿using QuadToSpine2D.Core.JsonConverters;

namespace QuadToSpine2D.Core.Data.Spine;

public class SpineJson
{
    //[JsonIgnore]
    public SpineSkeleton SpineSkeletons { get; set; } = new();
    //[JsonIgnore]
    public List<SpineBone> Bones { get; set; } = [];
    //[JsonIgnore]
    public List<SpineSlot> Slots { get; set; } = [];
    [JsonIgnore]
    public Dictionary<string, SpineSlot> SlotsDict { get; set; } = [];
    //[JsonIgnore]
    public List<Skin> Skins { get; set; } = [];

    //[JsonIgnore]
    public Dictionary<string, SpineAnimation> Animations { get; set; } = new();

    public void WriteToJson()
    {
        var setting = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver 
                { NamingStrategy = new CamelCaseNamingStrategy() },
            Formatting = GlobalData.IsReadableJson ? Formatting.Indented : Formatting.None
        };
        var spineJsonFile = JsonConvert.SerializeObject(this, setting);
        var output = Path.Combine(GlobalData.ResultSavePath, "Result.json");
        File.WriteAllText(output, spineJsonFile);
    }
}

public class SpineSkeleton
{
    public const string Spine = "3.8";
    public string ImagesPath = "";
}

public class SpineSlot
{
    public string Name { get; set; }
    public string Bone { get; set; }
    [JsonIgnore] public string Attachment { get; set; }
    [JsonIgnore] public int Order { get; set; }
}

public class Skin
{
    public string Name = string.Empty;

    [JsonConverter(typeof(AttachmentsJsonConverter<Attachments>))]
    public List<Attachments> Attachments = [];
}

public class Attachments
{
    public BaseMesh Value { get; set; }
}

public class BaseMesh
{
    public string Name { get; set; }
    [JsonIgnore] public Type CurrentType { get; set; }
}

public class Mesh : BaseMesh
{
    public string Type { get; set; } = "mesh";
    public float[] Uvs { get; set; } = new float[8];
    public float[] Triangles { get; set; } = [1, 2, 3, 1, 3, 0];
    public float[] Vertices { get; set; } = new float[8];
    public int Hull { get; set; } = 4;
}

public class LinkedMesh : BaseMesh
{
    public string Type { get; set; }

    //[JsonIgnore]
    public string Skin { get; set; }

    public string Parent { get; set; }
}

public class SpineBone
{
    public string Name { get; set; }
}

public class SpineAnimation
{
    //[JsonIgnore]
    public Dictionary<string, AnimationSlot> Slots { get; set; } = new();

    //[JsonIgnore]
    public Deform Deform { get; set; } = new();

    //[JsonIgnore]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<DrawOrder>? DrawOrder { get; set; } = [];
}

public class DrawOrder
{
    public float Time { get; set; }
    public List<DrawOrderOffset> Offsets { get; set; } = [];
}

public class DrawOrderOffset
{
    [JsonIgnore] public int SlotNum { get; set; } = 0;
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

[JsonConverter(typeof(SkinDeformConverter))]
public class Deform
{
    public Dictionary<string, Dictionary<string, AnimationDefault>> SkinName { get; set; } = new();
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