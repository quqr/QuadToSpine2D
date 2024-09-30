using QuadToSpine2D.Core.Data.Spine;

namespace QuadToSpine2D.Core.JsonConverters;

public class AnimationDefaultJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var obj = value as AnimationDefault;

        writer.WriteStartObject();

        writer.WritePropertyName(obj.Name);
        serializer.Serialize(writer, obj.ImageVertices);

        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer                          serializer)
    {
        throw new NotImplementedException();
    }

    public override bool CanConvert(Type objectType)
    {
        return true;
    }
}