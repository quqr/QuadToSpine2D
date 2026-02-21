using Newtonsoft.Json.Linq;
using QTSCore.Data.Quad;

namespace QTSCore.JsonConverters;

public class KeyframeJsonConverter : JsonConverter
{
    public static int IdCount = -1;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        IdCount++;
        var obj = serializer.Deserialize(reader);
        if (obj is not JObject jObject) return null;
        var json = new Keyframe
        {
            Name = jObject["name"]?.ToString(),
            Layers = jObject["layer"]?.ToObject<List<KeyframeLayer?>>(),
            Id = IdCount,
            AttachType = AttachType.Keyframe
        };
        if (json.Layers is not null)
            json.Order = jObject["orders"]?.ToObject<List<int>>() ?? Enumerable.Range(0, json.Layers.Count).ToList();
        return json;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Keyframe) == objectType;
    }
}