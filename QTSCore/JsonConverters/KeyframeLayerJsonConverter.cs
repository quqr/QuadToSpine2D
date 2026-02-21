using Newtonsoft.Json.Linq;
using QTSAvalonia.Helper;
using QTSCore.Data.Quad;
using QTSCore.Utility;

namespace QTSCore.JsonConverters;

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
        if (obj is not JObject jObject) return null;
        return new KeyframeLayer
        {
            Fog = ConvertToFog(jObject),
            TexId = jObject["tex_id"]?.ToObject<int>() ?? -1,
            Dstquad = ProcessUtility.MulFloats(jObject["dstquad"]?.ToObject<float[]>(),
                Instances.ConverterSetting.ScaleFactor),
            Srcquad = ProcessUtility.MulFloats(jObject["srcquad"]?.ToObject<float[]>(),
                Instances.ConverterSetting.ScaleFactor),
            BlendId = jObject["blend_id"]?.ToObject<int>() ?? -1,
            Attribute = ConvertToAttribute(jObject)
        };
    }

    private List<string>? ConvertToAttribute(JObject jObject)
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

    private List<string> ConvertToFog(JObject jObject)
    {
        var baseFog = jObject["fogquad"];
        List<string>? fog = [];
        switch (baseFog?.Type)
        {
            case JTokenType.Array:
                fog = baseFog.ToObject<List<string>>();
                break;
            case JTokenType.String:
                var result = baseFog.ToString();
                fog = [result, result, result, result];
                break;
        }

        if (fog is null || fog.Count == 0) fog = ["#ffffffff", "#ffffffff", "#ffffffff", "#ffffffff"];
        return fog;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(KeyframeLayer) == objectType;
    }
}