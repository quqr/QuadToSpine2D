﻿using Newtonsoft.Json.Serialization;
using QuadPlayer.Spine;

namespace QuadPlayer;

public class ProcessSpineJson
{
    private SpineJson _spineJson=new();
    private string _imagesPath = string.Empty;
    public string SpineJsonFile;
    public ProcessSpineJson(ProcessImage processImage,QuadJson quadJson)
    {
        Console.WriteLine("Writing spine json...");
        _imagesPath = processImage.SavePath;
        Init(processImage);
        ProcessAnimation(quadJson);
        ConvertToJson();
        
        Console.WriteLine("Finish");
    }

    private void Init(ProcessImage processImage)
    {
        _spineJson.Skeletons.ImagesPath = _imagesPath;
        _spineJson.Bones.Add(new Spine.Bone() { Name = "root"});
        _spineJson.Skins.Add(new Skin());
        for (int index = 0; index < processImage.ImagesData.Count; index++)
        {
            SetBaseData(processImage, index);
        }
    }

    private void SetBaseData(ProcessImage processImage, int index)
    {
        var layerGuid = processImage.ImagesData.Keys.ElementAt(index);
        var slotName = processImage.ImagesData[layerGuid].ImageName;
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

    private void ConvertToJson()
    {
        SpineJsonFile = JsonConvert.SerializeObject(_spineJson, Formatting.Indented,
            new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                    { NamingStrategy = new CamelCaseNamingStrategy() }
            });
    }

    private Dictionary<string, SpineAnimation> _spineAnimations { get; set; } = new();

    private void ProcessAnimation(QuadJson quad )
    {
        foreach (var skeleton in quad.Skeleton)
        {
            SetKeyframesData(quad, skeleton);
        }
        _spineJson.Animations = _spineAnimations;
    }

    private void SetKeyframesData(QuadJson quad, Skeleton? skeleton)
    {
        HashSet<string> keyframeLayerNames = [];
        List<DrawOrder> drawOrders = [];
        SpineAnimation spineAnimation = new();
        Deform deform = new();
            
        var timelines = quad.Animation
            .Find(x => x.ID == skeleton.Bone[0].Attach.ID).Timeline
            .Where(a => a.Attach.Type.Equals("keyframe")).ToList();
        
        var time = 0f;
        for (var index = 0; index < timelines.Count; index++)
        {
            AddKeyframe(quad, timelines[index], time, keyframeLayerNames, spineAnimation, deform, drawOrders);

            // 60 fps
            time += timelines[index].Time / 30f;
        }

        spineAnimation.Deform = deform;
        spineAnimation.DrawOrder = drawOrders;
        _spineAnimations[skeleton.Name] = spineAnimation;
    }

    private void AddKeyframe(QuadJson quad, Timeline timeline, float time, HashSet<string> keyframeLayerNames,
        SpineAnimation spineAnimation, Deform deform, List<DrawOrder> drawOrders)
    {
        var layers = quad.Keyframe.Find(x => x.ID == timeline.Attach.ID).Layer;
        DrawOrder drawOrder = new ()
        {
            Time = time
        };
        for (var index = 0; index < layers.Count; index++)
        {
            var layerName = layers[index].LayerName;
            AddAnimationAttachments(keyframeLayerNames, layerName, spineAnimation, time);

            AddAnimationVertices(time, deform, layerName, layers[index]);
            
            AddDrawOrderOffset(layerName, index, drawOrder);
        }

        //Set Order By Slot
        drawOrder.Offsets = drawOrder.Offsets.OrderBy(x => x.SlotNum).ToList();
        drawOrders.Add(drawOrder);
        RemoveNotExistsLayer(keyframeLayerNames, layers, spineAnimation, time);
    }

    private static void RemoveNotExistsLayer(HashSet<string> keyframeLayerNames,
        List<KeyframeLayer?> layers,
        SpineAnimation spineAnimation,
        float time)
    {
        //删除下一个时间没有的图层并且隐藏它
        var notContainsLayers = keyframeLayerNames
            .Where(x => !layers.Exists(y=>y.LayerName.Equals(x)))
            .ToList();
        foreach (var layerName in notContainsLayers)
        {
            if (layers.Exists(x => x.LayerName.Equals(layerName))) continue;
            spineAnimation.Slots[layerName].Attachment.Add(new AnimationAttachment
            {
                Time = time,
                Name = null
            });
            keyframeLayerNames.Remove(layerName);
        }
    }

    private static void AddAnimationVertices(float time, Deform deform, string layerName, KeyframeLayer layer)
    {
        AnimationVertices item = new()
        {
            Time = time
        };
        if (!deform.Default.TryGetValue(layerName, out var value))
        {
            value = new()
            {
                Name = layerName
            };
            deform.Default[layerName] = value;
        }
        item.Vertices = ProcessTools.MinusFloats(layer.Dstquad, layer.ZeroCenterPoints);
        value.ImageVertices.Add(item);
    }

    private static void AddAnimationAttachments(HashSet<string> keyframeLayerNames, string layerName, SpineAnimation spineAnimation,
        float time)
    {
        //添加成功说明此图层是新的或是被隐藏的
        if (!keyframeLayerNames.Add(layerName))
        {
            return;
        }
        //初始化slot
        if (!spineAnimation.Slots.TryGetValue(layerName, out var value))
        {
            value = new();
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
        var slotOrder = _spineJson.Slots.Find(x => x.Name == layerName)!.Order;
        var offset = index - slotOrder;
        var existLayer = drawOrder.Offsets.Find(x => x.SlotNum == slotOrder);
        if (existLayer != null)
        {
            existLayer.Offset = offset;
            return;
        }
        if(offset!=0)
        {
            drawOrder.Offsets.Add(new DrawOrderOffset
            {
                Slot = layerName,
                Offset = offset,
                SlotNum = slotOrder
            });
        }
    }
}