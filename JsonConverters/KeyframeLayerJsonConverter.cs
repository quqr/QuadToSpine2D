using Newtonsoft.Json.Linq;

namespace QuadPlayer.JsonConverters;

public class KeyframeLayerJsonConverter : JsonConverter
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
         var layer = new KeyframeLayer
         {
             Dstquad = jobj["dstquad"]?.ToObject<float[]>(),
             BlendId = jobj["blend_id"]?.ToObject<int>(),
             Attribute = jobj["attribute"]?.ToString(),
             Colorize = jobj["colorize"]?.ToString(),
             TexId = jobj["tex_id"]?.ToObject<int>(),
             Srcquad = jobj["srcquad"]?.ToObject<float[]>()
         };
         return layer;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(KeyframeLayer) == objectType;
    }
}