using Newtonsoft.Json.Linq;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core.JsonConverters;

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
        
        return new KeyframeLayer
        {
            TexId = jObject["tex_id"]?.ToObject<int>() ?? -1,
            Dstquad = ProcessUtility.MulFloats(jObject["dstquad"]?.ToObject<float[]>(),
                GlobalData.ScaleFactor),
            Srcquad = ProcessUtility.MulFloats(jObject["srcquad"]?.ToObject<float[]>(),
                GlobalData.ScaleFactor),
            BlendId = jObject["blend_id"]?.ToObject<int>() ?? -1,
            // Fog = ConvertToFog(jObject),
            // Attribute = ConvertToAttribute(jObject)
        };
    }

    private List<string>? ConvertToAttribute(JObject? jObject)
    {
        var baseAttribute = jObject["attribute"];
        List<string>? attribute = null;
        switch (baseAttribute?.Type)
        {
            case JTokenType.Array:
                attribute = baseAttribute.ToObject<List<string>>();
                break;
            case JTokenType.String:
                attribute = [baseAttribute.ToString()];
                break;
        }

        return attribute;
    }

    private List<string>? ConvertToFog(JObject? jObject)
    {
        var baseFog = jObject["fogquad"];
        List<string>? fog = null;
        switch (baseFog?.Type)
        {
            case JTokenType.Array:
                fog = baseFog.ToObject<List<string>>();
                break;
            case JTokenType.String:
                fog = [baseFog.ToString()];
                break;
        }

        return fog;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(KeyframeLayer) == objectType;
    }
}