using System.Collections.Frozen;
using QTSAvalonia.Helper;

namespace QTSCore.Data.Spine;

public class SpineJsonData
{
    //[JsonIgnore]
    public SpineSkeleton SpineSkeletons { get; set; } = new();

    //[JsonIgnore]
    public List<SpineBone> Bones { get; set; } = [];

    //[JsonIgnore]
    public List<SpineSlot> Slots { get; set; } = [];

    [JsonIgnore] public Dictionary<string, SpineSlot> SlotsDict { get; set; } = [];

    [JsonIgnore] public FrozenDictionary<string, SpineSlot> FrozenSlotsDict { get; set; }

    //[JsonIgnore]
    public List<Skin> Skins { get; set; } = [];

    //[JsonIgnore]
    public Dictionary<string, SpineAnimation> Animations { get; set; } = new();

    public string WriteToJson()
    {
        var setting = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.Indented
        };
        var spineJsonFile = JsonConvert.SerializeObject(this, setting);
        var output = Path.Combine(Instances.ConverterSetting.ResultSavePath, "Result.json");
        File.WriteAllText(output, spineJsonFile);
        LoggerHelper.Info($"{output} Complete!");
        return output;
    }
}
