using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace QuadPlayer.Spine;
public class SpineJson
{
    public Skeleton Skeletons = new();
    public List<Bone> Bones = new();
    public List<Slot> Slots= new();
    public List<Skin> Skins = new(1);
    public List<SpineAnimation> Animations = new();
}

public class Skeleton
{
    public string Spine="3.8.75";
    public string Images="";
}

public class Slot
{
    public string Name { get; set; }
    public string Bone { get; set; }
    public string Attachment { get; set; }
}

public class Skin
{
    public string Name = "default";

    [JsonConverter(typeof(AttachmentsJsonConverter<Attachments>))]
    public List<Attachments> Attachments = new();
}
public class Attachments
{
    public ImageSource Value{ get; set; }
}
public class ImageSource
{
    [JsonIgnore]
    public string Name;
    public float Width{ get; set; }
    public float Height{ get; set; }
}
public class Bone
{
    public string Name{ get; set; }
    public string Parent{ get; set; }
}
[JsonConverter(typeof(SpineAnimationJsonConverter))]
public class SpineAnimation
{
    [JsonIgnore] public int FPS = 60;
    [JsonIgnore] public string Name;
    public List<AnimationSlot> Slots= new();
    public List<AnimationBone> Bones= new();
}
public class SpineAnimationJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var obj = value as SpineAnimation;
        writer.WriteStartObject();
        writer.WritePropertyName("Name");
        writer.WriteValue(obj.Name); // Serialize the Name property

        // Serialize the Slots dictionary
        writer.WritePropertyName("Slots");
        writer.WriteStartObject();
        foreach (var slot in obj.Slots)
        {
            writer.WritePropertyName(slot.Name);
            serializer.Serialize(writer, slot.Attachment);
        }
        writer.WriteEndObject();

        // Serialize the Bones dictionary
        writer.WritePropertyName("Bones");
        writer.WriteStartObject();
        foreach (var bone in obj.Bones)
        {
            serializer.Serialize(writer, bone.Rotate);
            serializer.Serialize(writer, bone.Translate);
        }
        writer.WriteEndObject();

        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override bool CanConvert(Type objectType)
    {
        throw new NotImplementedException();
    }
}

public class AnimationSlot
{
    public string Name { get; set; }
    public List<AnimationAttachment> Attachment = new();
}

public class AnimationAttachment
{
    public float Time { get; set; }
    public string? Name { get; set; }
}

public class AnimationBone
{
    public List<Rotate> Rotate= new();
    public List<Translate> Translate= new();
}

public class Rotate
{
    public float Time{ get; set; }
    public float Angle{ get; set; }
}

public class Translate
{
    public float Time{ get; set; }
    public float X{ get; set; }
    public float Y{ get; set; }
}
public class DefaultAnimationKeyframe
{
    public List<AnimationAttachment> SliceImageData = new();
    public List<AnimationBone> AnimationBones = new();
}
public class AttachmentsJsonConverter<T> : JsonConverter<List<T>>
{
    public override void WriteJson(JsonWriter writer, List<T>? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        for (int i = 0; i < value.Count; i++)
        {
            var attachment = value[i] as Attachments;
            writer.WritePropertyName(attachment.Value.Name);
            writer.WriteStartObject();
            writer.WritePropertyName(attachment.Value.Name);
            serializer.Serialize(writer, attachment.Value);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    public override List<T>? ReadJson(JsonReader reader, Type objectType, List<T>? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}


