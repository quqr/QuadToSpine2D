﻿using System.Collections.Frozen;
using QuadToSpine2D.Core.Data.Spine;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core.Process;

public class ProcessSpineJson
{
    private readonly SpineJson _spineJson = new();
    private ProcessImage _processedImageData;
    private readonly Dictionary<string, SpineAnimation> _spineAnimations = [];
    private int _curSlotIndex;

    public SpineJson Process(ProcessImage processImage, QuadJson quadJson)
    {
        Console.WriteLine("Writing spine json...");
        GlobalData.LabelContent = "Writing spine json...";

        InitData(processImage);
        ProcessAnimation(quadJson);
        return _spineJson;
    }

    private void InitData(ProcessImage processImage)
    {
        _processedImageData = processImage;
        _spineJson.SpineSkeletons.ImagesPath = GlobalData.ImageSavePath;
        _spineJson.Bones.Add(new SpineBone { Name = "root" });

        for (var curFullSkinIndex = 0; curFullSkinIndex < processImage.ImageData.Count; curFullSkinIndex++)
        for (var texIdIndex = 0; texIdIndex < processImage.ImageData[curFullSkinIndex].Count; texIdIndex++)
        {
            var guids = processImage.ImageData[curFullSkinIndex][texIdIndex];
            if (guids is null) continue;
            _spineJson.Skins.Add(new Skin { Name = $"tex_id_{texIdIndex}/skin_{curFullSkinIndex}" });
            for (var guidIndex = 0; guidIndex < guids.Count; guidIndex++)
            {
                InitBaseData(guids.ElementAt(guidIndex).Value, curFullSkinIndex, texIdIndex, guidIndex);
                _curSlotIndex++;
            }
        }

        _spineJson.FrozenSlotsDict = _spineJson.SlotsDict.ToFrozenDictionary();
    }

    private void InitBaseData(LayerData layerData, int curFullSkin, int texIdIndex, int guidIndex)
    {
        var slotName = layerData.ImageName;
        layerData.SkinName = _spineJson.Skins.Last().Name;
        var spineSlot = new SpineSlot
            { Name = slotName, Attachment = slotName, Bone = "root", Order = _curSlotIndex };
        _spineJson.Slots.Add(spineSlot);
        _spineJson.SlotsDict[slotName] = spineSlot;
        //the first is mesh and it has animations
        if (curFullSkin == 0)
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
        //the linked mesh base on first mesh and animations also base on it
        else
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

    private void ProcessAnimation(QuadJson quad)
    {
        foreach (var skeleton in quad.Skeleton)
            SetKeyframesData(quad, skeleton);
        _spineJson.Animations = _spineAnimations;
    }

    private void SetKeyframesData(QuadJson quad, QuadSkeleton? skeleton)
    {
        List<Animation> animations = [];
        List<DrawOrder> drawOrders = [];
        SpineAnimation spineAnimation = new();
        Deform deform = new();
        
        animations
            .AddRange(skeleton.Bone
                .Select(bone => quad.Animation
                    .First(x => x.Id == bone.Attach.Id)));
        
        CombineAnimations(quad, animations, spineAnimation, deform, drawOrders);

        SetAnimationData(skeleton, drawOrders, spineAnimation, deform, animations);
    }

    private void CombineAnimations(
        QuadJson quad,
        List<Animation> animations,
        SpineAnimation spineAnimation,
        Deform deform,
        List<DrawOrder> drawOrders)
    {
        // Combine animations
        foreach (var animation in animations)
        {
            HashSet<string> keyframeLayerNames = [];
            var time = 0f;
            foreach (var timeline in animation.Timeline)
            {
                if (timeline.Attach is null) continue;
                List<KeyframeLayer>? layers = null;

                switch (timeline.Attach.AttachType)
                {
                    case AttachType.Keyframe:
                    {
                        layers = quad.Keyframe.FirstOrDefault(x => x.Id == timeline.Attach.Id)?.Layer; 
                        break;
                    }
                    case AttachType.Slot:
                    {
                        var attach = quad.Slot[timeline.Attach.Id].Attaches?.FirstOrDefault(x => x.AttachType.Equals("keyframe"));
                        if (attach is null) break;
                        layers = quad.Keyframe.FirstOrDefault(x => x.Id == attach.Id)?.Layer; 
                        break;
                    }
                }

                if (layers is not null)
                    AddKeyframe(layers, time, keyframeLayerNames, spineAnimation, deform, drawOrders,
                        timeline);
                // FPS : 60
                time += (timeline.Time + 1) / 60f;
            }
        }
    }

    private void SetAnimationData(QuadSkeleton skeleton, List<DrawOrder> drawOrders, SpineAnimation spineAnimation, Deform deform,
        List<Animation> animations)
    {
        drawOrders.RemoveAll(x => x.Offsets.Count == 0);
        spineAnimation.Deform = deform;
        
        // when write json ignore it if null 
        spineAnimation.DrawOrder = drawOrders.Count != 0 ? drawOrders : null;
        
        var animationName = skeleton.Name;
        if (GlobalData.IsRemoveUselessAnimations && deform.SkinName.Count == 0)
            return;
        
        if (animations.Any(x => x.IsLoop))
            animationName += "_LOOP";
        
        if (animations.Any(x=>x.Timeline.Any(y=>y.IsKeyframeMix)))
            animationName += "_MIX";
        
        _spineAnimations[animationName] = spineAnimation;
    }

    private void AddKeyframe(
        List<KeyframeLayer> layers,
        float time,
        HashSet<string> keyframeLayerNames,
        SpineAnimation spineAnimation,
        Deform deform,
        List<DrawOrder> drawOrders,
        Timeline timeline)
    {
        var drawOrder = new DrawOrder{Time = time} ;
        for (var index = 0; index < layers.Count; index++)
        {
            AddAnimationAttachments(keyframeLayerNames, layers[index].LayerName, spineAnimation, time);

            AddAnimationVertices(time, deform, layers[index], timeline);

            AddDrawOrderOffset(layers[index].LayerName, index, drawOrder);
        }

        // Order By Slot
        drawOrder.Offsets = drawOrder.Offsets.OrderBy(x => x.SlotNum).ToList();
        drawOrders.Add(drawOrder);
        
        RemoveNotExistsLayer(keyframeLayerNames, layers, spineAnimation, time);
    }
    
    /// <summary>
    /// Remove useless last keyframe layers, keeping next keyframe needs layers.
    /// </summary>
    private void RemoveNotExistsLayer(HashSet<string> keyframeLayerNames,
        List<KeyframeLayer> layers,
        SpineAnimation spineAnimation,
        float time)
    {
        //remove layers if they are not display next time
        var notContainsLayers = keyframeLayerNames
            .Where(x => !layers.Exists(y => y.LayerName.Equals(x)));
        foreach (var layerName in notContainsLayers)
        {
            spineAnimation.Slots[layerName].Attachment
                .Add(new AnimationAttachment
                {
                    Time = time,
                    Name = null
                });
            keyframeLayerNames.Remove(layerName);
        }
    }

    private void AddAnimationVertices(float time, Deform deform, KeyframeLayer layer, Timeline timeline)
    {
        AnimationVertices item = new()
        {
            Time = time
        };
        var skinName = _processedImageData.LayerDataDict[layer.LayerGuid].SkinName;

        if (!deform.SkinName.ContainsKey(skinName))
            deform.SkinName[skinName] = [];
        if (!deform.SkinName[skinName].TryGetValue(layer.LayerName, out var value))
        {
            value = new AnimationDefault
            {
                Name = layer.LayerName
            };
            deform.SkinName[skinName][layer.LayerName] = value;
        }

        item.Vertices = ProcessUtility.MinusFloats(layer.Dstquad, layer.ZeroCenterPoints);
        value.ImageVertices.Add(item);
        //Stepped animation
        if (!timeline.IsKeyframeMix)
        {
            value.ImageVertices.Add(new AnimationVertices
            {
                Time = item.Time + timeline.Time / 60f,
                Vertices = item.Vertices
            });
        }
    }

    private void AddAnimationAttachments(HashSet<string> keyframeLayerNames, string layerName,
        SpineAnimation spineAnimation,
        float time)
    {
        // It is new or be deleted if success
        if (!keyframeLayerNames.Add(layerName)) return;
        // Init slot
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
        var slotOrder = _spineJson.FrozenSlotsDict[layerName].Order;
        var offset = index - slotOrder;
        var existLayer = drawOrder.Offsets.FirstOrDefault(x => x.SlotNum == slotOrder);
        if (existLayer != null)
        {
            existLayer.Offset = offset;
            return;
        }
        // the offset 0 can be ignored
        if (offset == 0) return;
        drawOrder.Offsets.Add(new DrawOrderOffset
        {
            Slot = layerName,
            Offset = offset,
            SlotNum = slotOrder
        });
    }
}