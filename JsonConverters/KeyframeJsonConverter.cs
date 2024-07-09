using Newtonsoft.Json.Linq;
using QuadToSpine.Data.Quad;

namespace QuadToSpine.JsonConverters;

public class KeyframeJsonConverter : JsonConverter
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
        var jObject = obj as JObject;
        var keyframe = new Keyframe
        {
            Name = jObject["name"]?.ToString(),
            Layer = jObject["layer"]?.ToObject<List<KeyframeLayer?>>()
        };
        return keyframe;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Keyframe) == objectType;
    }
}