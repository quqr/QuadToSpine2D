using QuadToSpine.Data;
using QuadToSpine.Data.Quad;
using QuadToSpine.Data.Spine;

namespace QuadToSpine.Process;

public class ProcessSpineJson
{
    private readonly SpineJson _spineJson = new();
    private ProcessImage _processedImageData;

    public void Process(ProcessImage processImage, QuadJson quadJson)
    {
        Console.WriteLine("Writing spine json...");
        GlobalData.LabelContent = "Writing spine json...";

        InitData(processImage);
        ProcessAnimation(quadJson);
        GlobalData.LabelContent = "Finish";
        Console.WriteLine("Finish");
    }

    private int _curSlotIndex;

    private void InitData(ProcessImage processImage)
    {
        _processedImageData = processImage;
        _spineJson.SpineSkeletons.ImagesPath = GlobalData.ImageSavePath;
        _spineJson.Bones.Add(new SpineBone { Name = "root" });
        for (int curFullSkinIndex = 0; curFullSkinIndex < processImage.ImageData.Count; curFullSkinIndex++)
        {
            for (int texIdIndex = 0; texIdIndex < processImage.ImageData[curFullSkinIndex].Count; texIdIndex++)
            {
                var guids = processImage.ImageData[curFullSkinIndex][texIdIndex];
                if(guids is null)continue;
                _spineJson.Skins.Add(new Skin { Name = $"tex_id_{texIdIndex}/skin_{curFullSkinIndex}" });
                for (int guidIndex = 0; guidIndex < guids.Count; guidIndex++)
                {
                    InitBaseData(guids.ElementAt(guidIndex).Value,curFullSkinIndex,texIdIndex,guidIndex);
                    _curSlotIndex++;
                }
            }
        }
    }

    private void InitBaseData(LayerData layerData, int curFullSkin, int texIdIndex,int guidIndex)
    {
        var slotName = layerData.ImageName;
        layerData.SkinName = _spineJson.Skins.Last().Name;
        
        _spineJson.Slots.Add(new SpineSlot
            { Name = slotName, Attachment = slotName, Bone = "root", Order = _curSlotIndex});
        
        if (curFullSkin == 0)
        {
            _spineJson.Skins[texIdIndex].Attachments.Add(new Attachments
            {
                Value = new Mesh
                {
                    Name = slotName,
                    Uvs = layerData.UVs,
                    Vertices = layerData.ZeroCenterPoints,
                    CurrentType = typeof(Mesh)
                }
            });
        }
        else
        {
            _spineJson.Skins.Last().Attachments.Add(new Attachments
            {
                Value = new LinkedMesh
                {
                    Name = slotName,
                    Type = "linkedmesh",
                    Skin = $"tex_id_{texIdIndex}/skin_0",
                    Parent = _spineJson.Skins[texIdIndex].Attachments[guidIndex].Value.Name,
                    CurrentType = typeof(LinkedMesh)
                }
            });
        }
    }

    private void WriteToJson()
    {
        var spineJsonFile = JsonConvert.SerializeObject(_spineJson, Formatting.Indented,
            new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                    { NamingStrategy = new CamelCaseNamingStrategy() }
            });
        var output = Path.Combine(GlobalData.ResultSavePath, "Result.json");
        File.WriteAllText(output, spineJsonFile);
        Console.WriteLine(output);
    }

    private readonly Dictionary<string, SpineAnimation> _spineAnimations = new();

    private void ProcessAnimation(QuadJson quad)
    {
        foreach (var skeleton in quad.Skeleton)
        {
            SetKeyframesData(quad, skeleton);
        }

        ConvertToJson();
    }

    private void ConvertToJson()
    {
        _spineJson.Animations = _spineAnimations;
        WriteToJson();
        _spineAnimations.Clear();
    }

    private void SetKeyframesData(QuadJson quad, QuadSkeleton? skeleton)
    {
        HashSet<string> keyframeLayerNames = [];
        List<DrawOrder> drawOrders = [];
        SpineAnimation spineAnimation = new();
        Deform deform = new();
        List<Timeline> timelines = [];
        foreach (var bone in skeleton.Bone)
            timelines.AddRange(quad.Animation
                .Where(x => x.ID == bone.Attach.ID)
                .SelectMany(x => x.Timeline).ToList());

        var time = 0f;
        foreach (var timeline in timelines)
        {
            if (timeline.Attach is null) continue;
            switch (timeline.Attach.Type)
            {
                case "keyframe":
                {
                    var timeline1 = timeline;
                    var layers = quad.Keyframe.FirstOrDefault(x => x.ID == timeline1.Attach.ID)?.Layer;
                    if (layers is null) break;
                    AddKeyframe(layers, time, keyframeLayerNames, spineAnimation, deform, drawOrders);
                    break;
                }
                case "slot":
                {
                    var attach = quad.Slot[timeline.Attach.ID].Attaches?.FirstOrDefault(x => x.Type.Equals("keyframe"));
                    if (attach is null) break;
                    var layers = quad.Keyframe.FirstOrDefault(x => x.ID == attach.ID)?.Layer;
                    if (layers is null) break;
                    AddKeyframe(layers, time, keyframeLayerNames, spineAnimation, deform, drawOrders);
                    break;
                }
            }

            time += timeline.Time / 60f;
        }

        drawOrders.RemoveAll(x => x.Offsets.Count == 0);

        spineAnimation.Deform = deform;
        spineAnimation.DrawOrder = drawOrders.Count != 0 ? drawOrders : null;
        _spineAnimations[skeleton.Name] = spineAnimation;
    }

    private void AddKeyframe(List<KeyframeLayer> layers, float time, HashSet<string> keyframeLayerNames,
        SpineAnimation spineAnimation, Deform deform, List<DrawOrder> drawOrders)
    {
        DrawOrder drawOrder = new()
        {
            Time = time
        };
        for (var index = 0; index < layers.Count; index++)
        {
            AddAnimationAttachments(keyframeLayerNames, layers[index].LayerName, spineAnimation, time);

            AddAnimationVertices(time, deform, layers[index]);

            AddDrawOrderOffset(layers[index].LayerName, index, drawOrder);
        }

        //Set Order By Slot
        drawOrder.Offsets = drawOrder.Offsets.OrderBy(x => x.SlotNum).ToList();
        drawOrders.Add(drawOrder);
        RemoveNotExistsLayer(keyframeLayerNames, layers, spineAnimation, time);
    }

    private void RemoveNotExistsLayer(HashSet<string> keyframeLayerNames,
        List<KeyframeLayer?> layers,
        SpineAnimation spineAnimation,
        float time)
    {
        //删除下一个时间没有的图层并且隐藏它
        var notContainsLayers = keyframeLayerNames
            .Where(x => !layers.Exists(y => y.LayerName.Equals(x)))
            .ToList();
        foreach (var layerName in notContainsLayers)
        {
            spineAnimation.Slots[layerName].Attachment.Add(new AnimationAttachment
            {
                Time = time,
                Name = null
            });
            keyframeLayerNames.Remove(layerName);
        }
    }

    private void AddAnimationVertices(float time, Deform deform, KeyframeLayer layer)
    {
        AnimationVertices item = new()
        {
            Time = time
        };

        var skinName = _processedImageData.LayerDataDict[layer.LayerGuid].SkinName;
        if (!deform.SkinName.TryGetValue(skinName, out _))
        {
            deform.SkinName[skinName] = new Dictionary<string, AnimationDefault>();
        }
        if (!deform.SkinName[skinName].TryGetValue(layer.LayerName, out var value))
        {
            value = new AnimationDefault
            {
                Name = layer.LayerName
            };
            deform.SkinName[skinName][layer.LayerName] = value;
        }
        item.Vertices = ProcessTools.MinusFloats(layer.Dstquad, layer.ZeroCenterPoints);
        value.ImageVertices.Add(item);
    }

    private void AddAnimationAttachments(HashSet<string> keyframeLayerNames, string layerName,
        SpineAnimation spineAnimation,
        float time)
    {
        //添加成功说明此图层是新的或是被隐藏的
        if (!keyframeLayerNames.Add(layerName)) return;
        //初始化slot
        if (!spineAnimation.Slots.TryGetValue(layerName, out var value))
        {
            value = new AnimationSlot();
            spineAnimation.Slots[layerName] = value;
        }

        value.Attachment.Add(new AnimationAttachment
        {
            Time = time,
            Name = layerName
        });
    }

    private void AddDrawOrderOffset(string layerName, int index, DrawOrder drawOrder)
    {
        var slotOrder = _spineJson.Slots.First(x => x.Name == layerName).Order;
        var offset = index - slotOrder;
        var existLayer = drawOrder.Offsets.FirstOrDefault(x => x.SlotNum == slotOrder);
        if (existLayer != null)
        {
            existLayer.Offset = offset;
            return;
        }
        if (offset == 0)return;
        drawOrder.Offsets.Add(new DrawOrderOffset
        {
            Slot = layerName,
            Offset = offset, 
            SlotNum = slotOrder 
        });
    }
}