using Newtonsoft.Json.Serialization;
using QuadPlayer.Spine;

namespace QuadPlayer;

public class ProcessSpineJson
{
    private SpineJson _spineJson=new();
    public string SpineJsonFile;
    private Dictionary<string, string> _layerGuidAndSlotName = new();
    public ProcessSpineJson(ProcessImage processImage,QuadJson quadJson)
    {
        Console.WriteLine("Writing spine json...");
        Init(processImage);
        ProcessAnimation(quadJson,processImage);
        SpineJsonFile = ConvertToJson();
        Console.WriteLine("Finished");
    }

    private void Init(ProcessImage processImage)
    {
        _spineJson.Skeletons.Images = "D:/Download/quad_mobile_v05_beta-20240404-2000/quad_mobile_v05_beta/data/Output";
        _spineJson.Bones.Add(new Spine.Bone() { Name = "root"});
        _spineJson.Skins.Add(new Skin());
        for (int index = 0; index < processImage.ImagesData.Count; index++)
        {
            var layerGuid = processImage.ImagesData.Keys.ElementAt(index);
            var slotName = processImage.ImagesData[layerGuid].ImageName;
            _layerGuidAndSlotName[layerGuid] = slotName;
            _spineJson.Slots.Add(new Slot(){Name = slotName,Attachment = slotName ,Bone = "root"});
            _spineJson.Skins[0].Attachments.Add(new Attachments
            {
                Value = new Mesh
                {
                    Name = slotName,
                    Uvs = processImage.ImagesData[layerGuid].UVs,
                    Vertices =  processImage.ImagesData[layerGuid].SrcVertices,
                }
            });
        }

    }

    private string ConvertToJson()
    {
        var setting = new JsonSerializerSettings()
            { ContractResolver = new DefaultContractResolver() { NamingStrategy = new SnakeCaseNamingStrategy() } };
        var json = JsonConvert.SerializeObject(_spineJson,Formatting.Indented,setting);
        return json;
    }

    private Dictionary<string,SpineAnimation> spineAnimations = new();
    private void ProcessAnimation(QuadJson quad,ProcessImage processImage)
    {
        foreach (var skeleton in quad.Skeleton)
        {
            HashSet<KeyframeLayer> keyframeLayers = [];
            SpineAnimation spineAnimation = new();
            Deform deform = new();
            var timelines = quad.Animation
                .Find(x => x.ID == skeleton.Bone[0].Attach.ID).Timeline
                .Where(a => a.Attach.Type.Equals("keyframe")).ToList();
            float time=0;
            string layerName = "";
            foreach (var timeline in timelines)
            {
                // 60 fps
                time += timeline.Time*0.03334f;
                var layers = quad.Keyframe.Find(x => x.ID == timeline.Attach.ID).Layer;
                foreach (var layer in layers)
                {
                    layerName = processImage.ImagesData[layer.LayerGuid].ImageName;
                    //添加成功说明此图层是新的或是被删除的
                    if (keyframeLayers.Add(layer))
                    {
                        //初始化slot
                        if (!spineAnimation.Slots.TryGetValue(layerName, out AnimationSlot? value))
                        {
                            spineAnimation.Slots[layerName] = new();
                            spineAnimation.Slots[layerName].Attachment.Add(new AnimationAttachment
                            {
                                Time = time,
                                Name = layerName
                            });
                        }
                        //仅当上个图层被隐藏时，此时取消隐藏
                        else if(value.Attachment.Last().Name is null)
                        {
                            value.Attachment.Add(new AnimationAttachment
                            {
                                Time = time,
                                Name = layerName
                            });
                        }
                    }
                    AnimationVertices item = new();
                    item.Time = time;
                    if (!deform.Default.TryGetValue(layerName, out var vertex))
                    {
                        //如果没有则初始化
                        deform.Default[layerName] = new();
                        deform.Default[layerName].Name = layerName;
                        item.Time = 0;
                        item.Vertices = layer.Dstquad;
                    }
                    else
                    {
                        //此时顶点坐标是上个时间顶点坐标的偏移量，而不是绝对坐标
                        item.Vertices = ProcessTools.MinusFloats(layer.Dstquad, vertex.ImageVertices.Last().Vertices);
                    }
                    deform.Default[layerName].ImageVertices.Add(item);
                }
                //删除下一个时间没有的图层并且隐藏它
                keyframeLayers.Where(x => !layers.Contains(x)).ToList().ForEach(x =>
                {
                    spineAnimation.Slots[layerName].Attachment.Add(new AnimationAttachment
                    {
                        Time = time,
                        Name = null
                    });
                });
                keyframeLayers.RemoveWhere(x => !layers.Contains(x));
            }
            spineAnimation.Deform = deform;
            spineAnimations[skeleton.Name] = spineAnimation;
        }
        _spineJson.Animations = spineAnimations;
    }
    
}