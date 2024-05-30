using Newtonsoft.Json.Linq;

namespace QuadPlayer.JsonConverters;

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
        var layer = new KeyframeLayer();
        // layer.BlendID = jobj["blend_id"]?.ToObject<int>();
        // layer.Attribute = jobj["attribute"]?.ToString();
        // layer.Colorize = jobj["colorize"]?.ToString();
        layer.TexID = jObject["tex_id"]?.ToObject<int>() ?? 0;
        layer.Dstquad = ProcessTools.MulFloats(jObject["dstquad"]?.ToObject<float[]>(),
            TransmissionData.Instance.ScaleFactor);
        layer.Srcquad = ProcessTools.MulFloats(jObject["srcquad"]?.ToObject<float[]>(),
            TransmissionData.Instance.ScaleFactor);
        layer.BlendId = jObject["blend_id"]?.ToObject<int>() ?? 0;
        return layer;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(KeyframeLayer) == objectType;
    }
}