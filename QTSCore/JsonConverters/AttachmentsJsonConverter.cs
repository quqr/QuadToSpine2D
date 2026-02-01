using QTSCore.Data.Spine;

namespace QTSCore.JsonConverters;

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
            if (attachment.Mesh.GetType() == typeof(Mesh))
            {
                writer.WritePropertyName(attachment.Mesh.Name);
                writer.WriteStartObject();
                writer.WritePropertyName(attachment.Mesh.Name);
            }
            else if (attachment.Mesh.GetType() == typeof(LinkedMesh))
            {
                var parent = (attachment.Mesh as LinkedMesh).Parent;
                writer.WritePropertyName(parent);
                writer.WriteStartObject();
                writer.WritePropertyName(parent);
            }
            else
            {
                var cnt = attachment.Mesh as Boundingbox;
                writer.WritePropertyName(cnt.Name);
                writer.WriteStartObject();
                writer.WritePropertyName(cnt.Name);
            }

            serializer.Serialize(writer, attachment.Mesh);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    public override List<T>? ReadJson(JsonReader reader, Type objectType, List<T>? existingValue, bool hasExistingValue,
        JsonSerializer                           serializer)
    {
        throw new NotImplementedException();
    }
}