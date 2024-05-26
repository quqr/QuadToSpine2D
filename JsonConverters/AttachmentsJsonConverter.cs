using QuadPlayer.Spine;

namespace QuadPlayer.JsonConverters;

public class AttachmentsJsonConverter<T> : JsonConverter<List<T>>
{
    public override void WriteJson(JsonWriter writer, List<T>? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        for (var i = 0; i < value.Count; i++)
        {
            var attachment = value[i] as Attachments;
            writer.WritePropertyName(attachment.Value.Name);
            writer.WriteStartObject();
            writer.WritePropertyName(attachment.Value.Name);
            serializer.Serialize(writer, attachment.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    public override List<T>? ReadJson(JsonReader reader, Type objectType, List<T>? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}