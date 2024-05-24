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
        ProcessAnimation(quadJson);
        SpineJsonFile = ConvertToJson();
        Console.WriteLine("Finished");
    }

    private void Init(ProcessImage processImage)
    {
        _spineJson.Skeletons.Images = "D:/Download/quad_mobile_v05_beta-20240404-2000/quad_mobile_v05_beta/data/Output";
        _spineJson.Bones.Add(new Spine.Bone() { Name = "root"});
        _spineJson.Skins.Add(new Skin());
        for (int index = processImage.ImagesData.Count - 1; index >= 0; index--)
        {
            var layerGuid = processImage.ImagesData.Keys.ElementAt(index);
            var slotName = processImage.ImagesData[layerGuid].ImageName;
            _layerGuidAndSlotName[layerGuid] = slotName;
            _spineJson.Slots.Add(new Slot(){Name = slotName,Attachment = slotName ,Bone = "root",Order = index});
            _spineJson.Skins[0].Attachments.Add(new Attachments
            {
                Value = new Mesh
                {
                    Name = slotName,
                    Uvs = processImage.ImagesData[layerGuid].UVs,
                    Vertices =  processImage.ImagesData[layerGuid].ZeroCenterPoints,
                }
            });
        }
    }

    private string ConvertToJson()
    {
        var setting = new JsonSerializerSettings()
            { ContractResolver = new DefaultContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() } };
        var json = JsonConvert.SerializeObject(_spineJson,Formatting.Indented,setting);
        return json;
    }

    private Dictionary<string,SpineAnimation> spineAnimations = new();
    private void ProcessAnimation(QuadJson quad )
    {
        foreach (var skeleton in quad.Skeleton)
        {
            HashSet<string> keyframeLayerNames = [];
            SpineAnimation spineAnimation = new();
            Deform deform = new();
            List<DrawOrder> drawOrders = new();
            var timelines = quad.Animation
                .Find(x => x.ID == skeleton.Bone[0].Attach.ID).Timeline
                .Where(a => a.Attach.Type.Equals("keyframe")).ToList();
            float time = 0f;
            foreach (var timeline in timelines)
            {
                DrawOrder drawOrder = new DrawOrder();
                drawOrder.Time = time;
                var layers = quad.Keyframe.Find(x => x.ID == timeline.Attach.ID).Layer;
                foreach (var layer in layers)
                {
                    var layerName = layer.LayerName;
                    var slotOrder = _spineJson.Slots.Find(x => x.Name == layerName).Order;
                    var offset = layer.Order - slotOrder;
                    drawOrder.Offsets.Add(new DrawOrderOffset
                    {
                        Slot = layerName,
                        Offset = offset,
                    });
                    
                    //添加成功说明此图层是新的或是被隐藏的
                    if (keyframeLayerNames.Add(layerName))
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
                        else
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
                    if (!deform.Default.ContainsKey(layerName))
                    {
                        //如果没有则初始化
                        deform.Default[layerName] = new();
                        deform.Default[layerName].Name = layerName;
                        item.Time = 0;
                        item.Vertices = ProcessTools.MinusFloats(layer.Dstquad, layer.ZeroCenterPoints);
                    }
                    else
                    {
                        //此时顶点坐标是零点坐标的偏移量，而不是绝对坐标
                        item.Vertices = ProcessTools.MinusFloats(layer.Dstquad, layer.ZeroCenterPoints);
                    }
                    deform.Default[layerName].ImageVertices.Add(item);
                }
                //删除下一个时间没有的图层并且隐藏它
                var notContainsLayers = keyframeLayerNames.Where(x => !layers
                    .Exists(y=>y.LayerName.Equals(x))).ToList();
                foreach (var layerName in notContainsLayers)
                {
                    spineAnimation.Slots[layerName].Attachment.Add(new AnimationAttachment
                    {
                        Time = time,
                        Name = null
                    });
                    keyframeLayerNames.Remove(layerName);
                }
                // 60 fps
                time += timeline.Time * 0.01337f;
                drawOrders.Add(drawOrder);
            }
            spineAnimation.Deform = deform;
            spineAnimation.DrawOrder = drawOrders;
            spineAnimation.DrawOrder.Reverse();
            spineAnimations[skeleton.Name] = spineAnimation;
        }
        _spineJson.Animations = spineAnimations;
    }
    
}