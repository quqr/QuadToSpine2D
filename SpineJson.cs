using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace QuadPlayer.SpineJson;
public class SpineJson
{
    public Skeleton skeleton = new();
    public List<Bone> bones = new();
    public List<Slot> slots= new();
    public List<Skin> skins = new(1);
    public List<Animation> animations = new();

    public SpineJson(ProcessImage processImage)
    {
        DeleteUselessSymbol(ConvertToJson(processImage));
    }

    private string ConvertToJson(ProcessImage processImage)
    {
        skeleton.images = "D:/Download/quad_mobile_v05_beta-20240404-2000/quad_mobile_v05_beta/data/Test";
        bones.Add(new Bone() { name = "root"});
        skins.Add(new Skin());
        for (int index = 0; index < processImage.ClipImage.Count; index++)
        {
            bones.Add(new Bone(){name = index.ToString(),parent = "root"});
            slots.Add(new Slot(){name = index.ToString(),attachment = index.ToString(),bone = index.ToString()});
            skins[0].attachments.Add(new Attachments
            {
                value = new ImageSource
                {
                    name = index.ToString(),
                    value = new Image
                    {
                        name = index.ToString(),
                        x = 0,
                        y = 0,
                        width = processImage.ClipImage.Values.ElementAt(index).Width,
                        height = processImage.ClipImage.Values.ElementAt(index).Height
                    }
                }
            });
        }
        var settings = new JsonSerializerSettings() { ContractResolver = new MyWriteResolver() };
        var json = JsonConvert.SerializeObject(this,Formatting.Indented,settings);
        return json;
    }

    private static void DeleteUselessSymbol(string json)
    {
        var jsonLines = json.Split("\n");
        List<string> newJson = new();
        bool isSkin = false;
        bool skinOverd = false;
        bool continueNext = false;
        foreach (var jsonLine in jsonLines)
        {
            if (continueNext)
            {
                continueNext = false;
                Console.WriteLine("continueNext");
                continue;
            }
            if (isSkin)
            {
                string str;
                if (jsonLine.Contains("attachments"))
                {
                    str = "    \"attachments\": {";
                    continueNext = true;
                    Console.WriteLine(str);

                }
                else if (jsonLine.Contains("},"))
                {
                    str = ",";
                    continueNext = true;
                    Console.WriteLine(str);

                }
                else if(jsonLine.Contains("],"))
                {
                    isSkin = false;
                    str = jsonLine;
                    skinOverd = true;
                }
                else
                {
                    str = jsonLine;
                }
                newJson.Add(str);
                continue;
            }
            if (jsonLine.Contains("skins")&& !skinOverd)
            {
                isSkin = true;
                Console.WriteLine("Contains skins");
            }
            //Console.WriteLine(jsonLine);
            newJson.Add(jsonLine);
        }
        File.WriteAllLines("E:\\Asset\\Muramasa[PICTURE]\\WORK\\TestSpine\\t.json",newJson);
    }
}

public class Skeleton
{
    public string hash="";
    public string spine="3.8.75";
    public int x;
    public int y;
    public int width;
    public int height;
    public string images;
    public string audio;
}

public class Slot
{
    public string name { get; set; }
    public string bone { get; set; }
    public string attachment { get; set; }
}

public class Skin
{
    public string name = "default";
    public List<Attachments> attachments=new();
}

public class Attachments
{
    [Json(IsReplacedByValue = true)]
    public ImageSource value;
}

public class BaseImage
{
    [JsonIgnore]
    public string name;
}
public class ImageSource: BaseImage
{
    [Json(IsReplacedByValue = true)]
    public Image value;
}

public class Image:BaseImage
{
    public float x=0;
    public float y=0;
    public float width;
    public float height;
}
public class Bone
{
    public string name;
    public string parent;
}
public class Animation
{
    public List<AnimationBones> bones;
}

public class AnimationBones
{
    public AnimationBone bone;
}

public class AnimationBone
{
    public List<Rotate> rotate;
    public List<Translate> translate;
}

public class Rotate
{
    public float time;
    public float angle;
}

public class Translate
{
    public float time;
    public float x;
    public float y;
}



