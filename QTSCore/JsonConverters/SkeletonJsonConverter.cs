using Newtonsoft.Json.Linq;
using QTSCore.Data.Quad;

namespace QTSCore.JsonConverters;

public class SkeletonJsonConverter : JsonConverter
{
    public static int IdCount = -1;


    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        IdCount++;
        var obj = serializer.Deserialize(reader);
        if (obj is not JObject jObject) return null;
        var skeleton = new QuadSkeleton
        {
            Name = jObject["name"]?.ToString(), Bone = jObject["bone"]?.ToObject<QuadBone[]>(), Id = IdCount,
            AttachType = AttachType.Skeleton
        };
        return skeleton;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(QuadSkeleton) == objectType;
    }
}