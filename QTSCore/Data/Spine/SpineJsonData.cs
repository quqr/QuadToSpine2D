﻿using System.Collections.Frozen;
using QuadToSpine2D.Core.JsonConverters;

namespace QuadToSpine2D.Core.Data.Spine;

public class SpineJsonData
{
    //[JsonIgnore]
    public SpineSkeleton SpineSkeletons { get; set; } = new();

    //[JsonIgnore]
    public List<SpineBone> Bones { get; set; } = [];

    //[JsonIgnore]
    public List<SpineSlot> Slots { get; set; } = [];

    [JsonIgnore] public Dictionary<string, SpineSlot> SlotsDict { get; set; } = [];

    [JsonIgnore] public FrozenDictionary<string, SpineSlot> FrozenSlotsDict { get; set; }

    //[JsonIgnore]
    public List<Skin> Skins { get; set; } = [];

    //[JsonIgnore]
    public Dictionary<string, SpineAnimation> Animations { get; set; } = new();

    public void WriteToJson()
    {
        GlobalData.BarValue = 95;

        var setting = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
                { NamingStrategy = new CamelCaseNamingStrategy() },
            Formatting = GlobalData.IsReadableJson ? Formatting.Indented : Formatting.None
        };
        var spineJsonFile = JsonConvert.SerializeObject(this, setting);
        var output        = Path.Combine(GlobalData.ResultSavePath, "Result.json");
        File.WriteAllText(output, spineJsonFile);
        Console.WriteLine("Complete!");
    }
}

public class SpineSkeleton
{
    public string Spine      { get; set; } = "3.8";
    public string ImagesPath { get; set; } = string.Empty;
}

public class SpineSlot
{
    public              string Name             { get; set; }
    public              string Bone             { get; set; } = "root";
    public              string Blend            { get; set; }
    [JsonIgnore] public string Attachment       { get; set; }
    [JsonIgnore] public int    SlotOrder        { get; set; }
    [JsonIgnore] public int    OrderByImageSlot { get; set; }
}

public class Skin
{
    public string Name { get; set; } = string.Empty;

    [JsonConverter(typeof(AttachmentsJsonConverter<Attachments>))]
    public List<Attachments> Attachments { get; set; } = [];
}

public class Attachments
{
    public BaseMesh Mesh { get; set; }
}

public class BaseMesh
{
    [JsonIgnore] public string Name { get; set; }
}

public class Boundingbox : BaseMesh
{
    public string  Type        { get; set; } = "boundingbox";
    public int     VertexCount { get; set; } = 4;
    public float[] Vertices    { get; set; } = new float[8];
}

public class Mesh : BaseMesh
{
    public string  Type      { get; set; } = "mesh";
    public float[] Uvs       { get; set; } = new float[8];
    public float[] Triangles { get; set; } = [1, 2, 3, 1, 3, 0];
    public float[] Vertices  { get; set; } = new float[8];
    public int     Hull      { get; set; } = 4;
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
    public string Name   { get; set; }
    public int    ScaleY { get; set; } = -1;
}

public class SpineAnimation
{
    //[JsonIgnore]
    public Dictionary<string, AnimationSlot> Slots { get; set; } = [];

    //[JsonIgnore]
    public Deform Deform { get; set; } = new();

    //[JsonIgnore]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<DrawOrder>? DrawOrder { get; set; }
}

public class DrawOrder
{
    public float                 Time    { get; set; }
    public List<DrawOrderOffset> Offsets { get; set; } = [];

    [JsonIgnore] public List<LayerOffset> LayerOffsets { get; set; } = [];

    public void SortOffset()
    {
        // bugs
        var isResetOffsetList = new List<bool>();
        foreach (var layerOffset in LayerOffsets)
        {
            var slotOrder = layerOffset.LayerSlotOrder;
            var offset    = layerOffset.LayerIndex - slotOrder;

            if (offset >= 0)
            {
                if (slotOrder - isResetOffsetList.Count == 0) offset = 0;
                isResetOffsetList.Add(true);
                if (offset == 0) continue;
            }
            else if (layerOffset.LayerIndex == 0 || isResetOffsetList[^1])
            {
                isResetOffsetList.Add(false);
            }

            Offsets.Add(new DrawOrderOffset
            {
                Slot    = layerOffset.LayerName,
                Offset  = offset,
                SlotNum = slotOrder
            });
        }

        Offsets.Sort((x, y) => x.SlotNum.CompareTo(y.SlotNum));
    }

#region Nested type: LayerOffset

    public class LayerOffset
    {
        public              string    LayerName      { get; set; }
        [JsonIgnore] public SpineSlot Slot           { get; set; }
        public              int       LayerSlotOrder => Slot.SlotOrder;
        public              int       LayerIndex     { get; set; }
    }

#endregion
}

public class DrawOrderOffset
{
    [JsonIgnore] public int    SlotNum { get; set; }
    public              string Slot    { get; set; } = string.Empty;
    public              int    Offset  { get; set; }
}

public class AnimationSlot
{
    public List<AnimationAttachment> Attachment { get; set; } = [];
}

public class AnimationAttachment
{
    public float   Time { get; set; }
    public string? Name { get; set; }
}

[JsonConverter(typeof(SkinDeformConverter))]
public class Deform
{
    // {skinName:{slotName:{value}}}
    public Dictionary<string, Dictionary<string, AnimationDefault>> SkinName { get; set; } = new();

    public Deform Clone()
    {
        return new Deform
        {
            SkinName = new Dictionary<string, Dictionary<string, AnimationDefault>>(SkinName)
        };
    }
}

[JsonConverter(typeof(AnimationDefaultJsonConverter))]
public class AnimationDefault
{
    // mesh name
    public string                  Name          { get; set; }
    public List<AnimationVertices> ImageVertices { get; set; } = [];
}

public class AnimationVertices
{
    public float   Time     { get; set; }
    public float[] Vertices { get; set; } = new float[8];
}