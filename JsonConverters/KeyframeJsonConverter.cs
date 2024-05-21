using Newtonsoft.Json.Linq;

namespace QuadPlayer.JsonConverters;

public class KeyframeJsonConverter: JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var obj = serializer.Deserialize(reader);
        if (obj.GetType()!=typeof(JObject)) return null;
        var jobj = obj as JObject;
        var keyframe = new Keyframe
        {
            Name = jobj["name"]?.ToString(),
            Layer = jobj["layer"]?.ToObject<List<KeyframeLayer?>>()
        };
        return keyframe;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Keyframe) == objectType;
    }
}