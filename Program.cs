using System.Reflection;
using System.Text;
using Newtonsoft.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using QuadPlayer.SpineJson;
using Image = QuadPlayer.SpineJson.Image;

namespace QuadPlayer;

class Program
{
    static void Main(string[] args)
    {
        ProcessQuadFile processQuadFile = new();
        ProcessImage processImage = new(processQuadFile.Quad);
        SpineJson.SpineJson spineJson = new(processImage);

    }
}
public class MyWriteResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, memberSerialization);
        var attr = member.GetCustomAttribute(typeof(JsonAttribute)) as JsonAttribute;
        if (attr != null)
        {
            prop.ShouldSerialize = instance =>
            {
                var val = prop.ValueProvider.GetValue(instance) as BaseImage;
                prop.PropertyName = val.name;
                return true;
            };
        }
        return prop;
    }
}