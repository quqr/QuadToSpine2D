using Newtonsoft.Json.Linq;
using QuadToSpine.Data.Quad;

namespace QuadToSpine.JsonConverters;

public class SkeletonJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var obj = serializer.Deserialize(reader);
        if (obj.GetType() != typeof(JObject)) return null;
        var jobj = obj as JObject;
        var skeleton = new QuadSkeleton
        {
            Name = jobj["name"]?.ToString(),
            Bone = jobj["bone"]?.ToObject<List<QuadBone>>()
        };
        return skeleton;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(QuadSkeleton) == objectType;
    }
}