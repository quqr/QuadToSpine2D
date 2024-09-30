using Newtonsoft.Json.Linq;

namespace QuadToSpine2D.Core.JsonConverters;

public class SkeletonJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer                          serializer)
    {
        var obj = serializer.Deserialize(reader);
        if (obj is not JObject jObject) return null;
        var skeleton = new QuadSkeleton
        {
            Name = jObject["name"]?.ToString(),
            Bone = jObject["bone"]?.ToObject<List<QuadBone>>()
        };
        return skeleton;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(QuadSkeleton) == objectType;
    }
}