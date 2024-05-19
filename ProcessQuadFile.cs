using Newtonsoft.Json.Linq;

namespace QuadPlayer;

public class ProcessQuadFile
{
    public QuadJson Quad = new();
    public ProcessQuadFile()
    {
        string quadJsonFile = File.ReadAllText("D:\\Download\\quad_mobile_v05_beta-20240404-2000\\quad_mobile_v05_beta\\data\\Amiguchi00.json");
        var x = JsonConvert.DeserializeObject<QuadJson>(quadJsonFile);
        return;
        //JObject jsonObj = JObject.Parse(quadJsonFile);
        //var keyframes = jsonObj["keyframe"];
        //var animations = jsonObj["animation"];
        //ParseKeyframes(keyframes, Quad);
        //ParseAnimation(animations, Quad);
    }

    void ParseKeyframes(JToken keyframes,QuadJson quadJson)
    {
        for (int i = 0; i < keyframes.Count(); i++)
        {
            Keyframe keyframe = new Keyframe
            {
                Debug = keyframes[i]["debug"].ToString(),
                Name = keyframes[i]["name"].ToString(),
                Order = i,
            };
            var layers = new List<KeyframeLayer?>();
            var points = new[]{float.MaxValue,float.MaxValue,-1,-1};
            var isNoLayer = false;
            for (int j = 0; j < keyframes[i]["layer"].Count(); j++)
            {
                var layer = new KeyframeLayer();
                if (keyframes[i]["layer"][j].ToString().Equals("0"))
                {
                    isNoLayer = true;
                    break;
                }
                layer.Debug = keyframes[i]["layer"][j]?["debug"]?.ToObject<List<string>>();
                layer.BlendID = keyframes[i]["layer"][j]?["blend_id"]?.ToObject<int?>();
                layer.Fogquad = keyframes[i]["layer"][j]?["fogquad"]?.ToString();
                layer.TexID = keyframes[i]["layer"][j]?["tex_id"]?.ToObject<int?>();
                layer.Srcquad = keyframes[i]["layer"][j]?["srcquad"]?.ToObject<float[]>();
                layer.Dstquad = keyframes[i]["layer"][j]?["dstquad"]?.ToObject<float[]>();
                layer.Attribute = keyframes[i]["layer"][j]?["attribute"]?.ToString();
                layer.Colorize = keyframes[i]["layer"][j]?["colorize"]?.ToString();
                points = ProcessTools.FindMinAndMaxPoints(points, layer.Dstquad);
                layer.CalculateRotate();
                ProcessSrcquadLayerGUID(layer);
                layers.Add(layer);
            }
            if (!isNoLayer)
            {
                keyframe.Layers = layers;
                keyframe.CalculateRect(points);
                quadJson.Keyframe.Add(keyframe);
            }
        }
    }

    private void ProcessSrcquadLayerGUID(KeyframeLayer layer)
    {
        if(layer.Srcquad is null) return;
        layer.LayerGUID = layer.Srcquad.Sum(x=>x/7).ToString();
    }

    void ParseAnimation(JToken animations, QuadJson quadJson)
    {
        for (int i = 0; i < animations.Count(); i++)
        {
            var animation = new Animation();
            animation.Name = animations[i]["name"].ToString();
            animation.LoopId = animations[i]["loop_id"]?.ToObject<int>();
            for (int j = 0; j < animations[i]["timeline"].Count(); j++)
            {
                var timeline = new Timeline();
                timeline.Time = animations[i]["timeline"][j]["time"].ToObject<int>();
                timeline.Color = animations[i]["timeline"][j]["color"]?.ToString();
                timeline.Matrix = animations[i]["timeline"][j]["matrix"]?.ToObject<float[]>();
                timeline.MatrixMix = IntToBool(animations[i]["timeline"][j]["matrix_mix"]?.ToObject<int>());
                timeline.ColorMix = IntToBool(animations[i]["timeline"][j]["color_mix"]?.ToObject<int>());
                timeline.KeyframeMix = IntToBool(animations[i]["timeline"][j]["keyframe_mix"]?.ToObject<int>());
                timeline.HitboxMix = IntToBool(animations[i]["timeline"][j]["hitbox_mix"]?.ToObject<int>());
                timeline.Attach = new Attach()
                {
                    Type = animations[i]["timeline"][j]["attach"]["type"].ToString(),
                    ID = animations[i]["timeline"][j]["attach"]["id"].ToObject<int>()
                };
                animation.Timeline.Add(timeline);
            }
            Quad.Animation.Add(animation);
        }
    }

    bool IntToBool(int? i)
    {
        if (i is null) return false;
        return i != 0;
    }
}