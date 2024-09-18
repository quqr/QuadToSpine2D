using QuadToSpine2D.Core.Data.Spine;

namespace QuadToSpine2D.Core.JsonConverters;

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
        foreach (var v in value)
        {
            var attachment = v as Attachments;
            if (attachment.Value.GetType() == typeof(Mesh))
            {
                writer.WritePropertyName(attachment.Value.Name);
                writer.WriteStartObject();
                writer.WritePropertyName(attachment.Value.Name);
                serializer.Serialize(writer, attachment.Value);
                writer.WriteEndObject();
            }
            else if(attachment.Value.GetType() == typeof(LinkedMesh))
            {
                var parent = (attachment.Value as LinkedMesh).Parent;
                writer.WritePropertyName(parent);
                writer.WriteStartObject();
                writer.WritePropertyName(parent);
                serializer.Serialize(writer, attachment.Value);
                writer.WriteEndObject();
            }
            else
            {
                var cnt = attachment.Value as BoundingBox;
                writer.WritePropertyName(cnt.Name);
                writer.WriteStartObject();
                writer.WritePropertyName(cnt.Name);
                serializer.Serialize(writer, attachment.Value);
                writer.WriteEndObject();
            }
        }

        writer.WriteEndObject();
    }

    public override List<T>? ReadJson(JsonReader reader, Type objectType, List<T>? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}