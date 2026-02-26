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
        JsonSerializer                          serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType != JsonToken.StartObject)
        {
            reader.Skip();
            return null;
        }

        IdCount++;
        var jObject = JObject.Load(reader);
        
        return ConvertToKeyframe(jObject);
    }

    private Keyframe ConvertToKeyframe(JObject jObject)
    {
        // 使用 Value<T>() 替代 ToString() 和 ToObject<T>()
        var layers = jObject["layer"]?.ToObject<KeyframeLayer?[]?>();
        
        var json = new Keyframe
        {
            Name       = jObject["name"]?.Value<string>() ?? string.Empty,
            Layers     = layers,
            Id         = IdCount,
            AttachType = AttachType.Keyframe
        };

        // 优化 Order 生成逻辑
        if (layers?.Length > 0)
        {
            json.Order = jObject["orders"]?.ToObject<int[]>() 
                         ?? Enumerable.Range(0, layers.Length).ToArray();
        }

        return json;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Keyframe);
    }
}