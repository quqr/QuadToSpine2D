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
        JsonSerializer                          serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        // 检查当前 token 是否为对象
        if (reader.TokenType != JsonToken.StartObject)
        {
            // 如果不是对象（如Integer），跳过或返回默认值
            reader.Skip();
            return null;
        }

        var jObject = JObject.Load(reader);
        return ConvertToKeyframeLayer(jObject);
    }

    private KeyframeLayer ConvertToKeyframeLayer(JObject jObject)
    {
        var scaleFactor = Instances.ConverterSetting.ScaleFactor;
        
        return new KeyframeLayer
        {
            Fog = ConvertToFog(jObject),
            TexId = jObject["tex_id"]?.Value<int>() ?? -1,
            Dstquad = ProcessUtility.MulFloats(
                jObject["dstquad"]?.ToObject<float[]?>(), scaleFactor)!,
            Srcquad = ProcessUtility.MulFloats(
                jObject["srcquad"]?.ToObject<float[]?>(), scaleFactor),
            BlendId = jObject["blend_id"]?.Value<int>() ?? -1,
            Attribute = ConvertToAttribute(jObject),
            Colorize = jObject["colorize"]?.Value<string>() ?? string.Empty,
            AttachType = AttachType.KeyframeLayer
        };
    }

    private string[] ConvertToAttribute(JObject jObject)
    {
        var baseAttribute = jObject["attribute"];
        
        return baseAttribute?.Type switch
        {
            JTokenType.Array => baseAttribute.ToObject<string[]?>() ?? [],
            JTokenType.String => [baseAttribute.Value<string>()],
            _ => []
        };
    }

    private string[] ConvertToFog(JObject jObject)
    {
        var baseFog = jObject["fogquad"];

        var result = baseFog?.Type switch
        {
            JTokenType.Array => baseFog.ToObject<string[]?>(),
            JTokenType.String =>
            [
                baseFog.Value<string>(), 
                baseFog.Value<string>(), 
                baseFog.Value<string>(), 
                baseFog.Value<string>()
            ],
            _ => null
        };

        return result?.Length > 0 ? result : ["#ffffffff", "#ffffffff", "#ffffffff", "#ffffffff"];
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(KeyframeLayer);
    }
}