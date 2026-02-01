using QTSCore.Data.Spine;

namespace QTSCore.JsonConverters;

public class SkinDeformConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var obj = value as Deform;
        writer.WriteStartObject();
        foreach (var name in obj.SkinName)
        {
            writer.WritePropertyName(name.Key);
            serializer.Serialize(writer, name.Value);
        }

        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer                          serializer)
    {
        throw new NotImplementedException();
    }

    public override bool CanConvert(Type objectType)
    {
        throw new NotImplementedException();
    }
}