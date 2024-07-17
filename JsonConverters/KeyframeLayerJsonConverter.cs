using Newtonsoft.Json.Linq;
using QuadToSpine.Data;
using QuadToSpine.Data.Quad;

namespace QuadToSpine.JsonConverters;

public class KeyframeLayerJsonConverter : JsonConverter
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
        var layer = new KeyframeLayer
        {
            // layer.BlendID = jobj["blend_id"]?.ToObject<int>();
            // layer.Attribute = jobj["attribute"]?.ToString();
            // layer.Colorize = jobj["colorize"]?.ToString();
            TexId = jObject["tex_id"]?.ToObject<int>() ?? 0,
            Dstquad = ProcessTools.MulFloats(jObject["dstquad"]?.ToObject<float[]>(),
                GlobalData.ScaleFactor),
            Srcquad = ProcessTools.MulFloats(jObject["srcquad"]?.ToObject<float[]>(),
                GlobalData.ScaleFactor),
            BlendId = jObject["blend_id"]?.ToObject<int>() ?? 0
        };
        return layer;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(KeyframeLayer) == objectType;
    }
}