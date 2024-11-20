﻿using Newtonsoft.Json.Linq;

namespace QuadToSpine2D.Core.JsonConverters;

public class HitboxJsonConverter : JsonConverter
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
        var json = new Hitbox
        {
            Name = jObject["name"]?.ToString(),
            Layer = jObject["layer"]?.ToObject<List<HitboxLayer?>>(),
            Id = IdCount,
            AttachType = AttachType.HitBox
        };
        return json;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Hitbox) == objectType;
    }
}