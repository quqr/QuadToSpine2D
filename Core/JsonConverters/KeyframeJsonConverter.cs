using Newtonsoft.Json.Linq;

namespace QuadToSpine2D.Core.JsonConverters;

public class KeyframeJsonConverter : JsonConverter
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
        return new Keyframe
        {
            Name   = jObject["name"]?.ToString(),
            Layers = jObject["layer"]?.ToObject<List<KeyframeLayer?>>(),
            AttachType = AttachType.Keyframe
        };
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Keyframe) == objectType;
    }
}