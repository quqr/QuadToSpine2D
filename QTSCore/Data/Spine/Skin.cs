using QTSCore.JsonConverters;

namespace QTSCore.Data.Spine;

public class Skin
{
    public string Name { get; set; } = string.Empty;

    [JsonConverter(typeof(AttachmentsJsonConverter<Attachments>))]
    public List<Attachments> Attachments { get; set; } = [];
}
