using Newtonsoft.Json.Linq;

namespace QuadPlayer.JsonConverters;

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
        var skeleton = new Skeleton
        {
            Name = jobj["name"]?.ToString(),
            Bone = jobj["bone"]?.ToObject<List<Bone>>()
        };
        return skeleton;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Skeleton) == objectType;
    }
}