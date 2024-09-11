using System.Collections.Concurrent;
using System.Collections.Frozen;
using QuadToSpine2D.Core.Data.Spine;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core.Process;

public class ProcessSpineJson
{
    private readonly SpineJson _spineJson;
    private ProcessImage _processedImageData;
    private readonly ConcurrentDictionary<string, SpineAnimation> _spineAnimations;
    private const float Fps = 1 / 60f;

    public ProcessSpineJson()
    {
        _spineJson = new SpineJson();
        _spineAnimations = [];
    }

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
        //AddBoundingBox();
        for (var curFullSkinIndex = 0; curFullSkinIndex < processImage.ImageData.Count; curFullSkinIndex++)
        {
            for (var texIdIndex = 0; texIdIndex < processImage.ImageData[curFullSkinIndex].Count; texIdIndex++)
            {
                var layerNames = processImage.ImageData[curFullSkinIndex][texIdIndex];
                if (layerNames is null) continue;
                _spineJson.Skins.Add(new Skin { Name = $"tex_id_{texIdIndex}/skin_{curFullSkinIndex}" });
                for (var layerNameIndex = 0; layerNameIndex < layerNames.Count; layerNameIndex++)
                {
                    InitBaseData(layerNames.ElementAt(layerNameIndex).Value, curFullSkinIndex, texIdIndex, layerNameIndex);
                }
            }
        }
        
        OrderSlots();

        _spineJson.FrozenSlotsDict = _spineJson.SlotsDict.ToFrozenDictionary();
    }
    
    private void OrderSlots()
    {
        _spineJson.Slots = _spineJson.Slots
            .OrderBy(x => x.OrderId)
            .ToList();
        for (var index = 0; index < _spineJson.Slots.Count; index++)
        {
            _spineJson.Slots[index].Order = index;
        }
    }

    private void AddBoundingBox()
    {
        _spineJson.Skins.Add(new Skin
        {
            Name = "default", Attachments =
            [
                new Attachments
                {
                    Value = new BoundingBox
                    {
                        Name = "boundingbox",
                        Type = "boundingbox",
                        Vertices = [0, 0, 0, 0, 0, 0, 0, 0],
                    }
                }
            ]
        });
    }

    private void InitBaseData(LayerData layerData, int curFullSkin, int texIdIndex, int guidIndex)
    {
        var slotName = layerData.ImageName;
        layerData.SkinName = _spineJson.Skins.Last().Name;
        var spineSlot = new SpineSlot
        {
            Name = slotName, Attachment = slotName, Bone = "root",
            OrderId = layerData.KeyframeLayer.OrderId
        };
        _spineJson.Slots.Add(spineSlot);
        _spineJson.SlotsDict[slotName] = spineSlot;
        //the first is mesh and it has animations
        if (curFullSkin == 0)
        {
            _spineJson.Skins[texIdIndex].Attachments.Add(new Attachments
            {
                Value = new Mesh
                {
                    Name = slotName,
                    Uvs = layerData.KeyframeLayer.UVs,
                    Vertices = layerData.KeyframeLayer.ZeroCenterPoints,
                    CurrentType = typeof(Mesh)
                }
            });
            return;
        }

        //the linked mesh base on first mesh and animations also base on it
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
        //Parallel.ForEach(quad.Skeleton, skeleton => { SetKeyframesData(quad, skeleton); });
        _spineJson.Animations = _spineAnimations.ToDictionary();
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
            var keyframeLayerNames = new HashSet<string>();
            var time = 0f;
            foreach (var timeline in animation.Timeline)
            {
                if (timeline.Attach is null) continue;
                var layers = GetKeyframeLayers(quad, timeline);
                if (layers is not null)
                    AddKeyframe(layers, time, keyframeLayerNames, spineAnimation, deform, drawOrders,
                        timeline);
                // FPS : 60
                time += (timeline.Time + 1) / 60f;
            }
        }
    }

    private List<KeyframeLayer>? GetKeyframeLayers(QuadJson quad, Timeline timeline)
    {
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
                var attach = quad.Slot[timeline.Attach.Id].Attaches
                    ?.FirstOrDefault(x => x.AttachType == AttachType.Keyframe);
                if (attach is null) break;
                layers = quad.Keyframe.FirstOrDefault(x => x.Id == attach.Id)?.Layer;
                break;
            }
            default:
                //Console.WriteLine($"Can Not Process Animation Attach Type : {timeline.Attach.AttachType}");
                break;
        }

        return layers;
    }

    private void SetAnimationData(
        QuadSkeleton skeleton,
        List<DrawOrder> drawOrders,
        SpineAnimation spineAnimation,
        Deform deform,
        List<Animation> animations)
    {
        var animationName = skeleton.Name;
        if (deform.SkinName.Count == 0)
        {
            if (!GlobalData.IsRemoveUselessAnimations)
                animationName += "_USELESS";
            else
                return;
        }

        if (animations.Any(x => x.IsLoop))
            animationName += "_LOOP";
        var mixInfo = string.Empty;
        if (animations.Any(x => x.Timeline.Any(y =>
            {
                if (y.IsKeyframeMix)
                {
                    mixInfo = "_KeyframeMix";
                    return true;
                }

                if (!y.IsMatrixMix) return false;
                mixInfo = "_MatrixMix";
                return true;
            })))
            animationName += mixInfo;

        drawOrders.RemoveAll(x => x.Offsets.Count == 0);
        spineAnimation.Deform = deform;

        // when write json, ignore it if null 
        spineAnimation.DrawOrder = drawOrders.Count != 0 ? drawOrders : null;

        _spineAnimations[animationName] = spineAnimation;
    }

    private void AddKeyframe(
        List<KeyframeLayer> layers,
        float initTime,
        HashSet<string> keyframeLayerNames,
        SpineAnimation spineAnimation,
        Deform deform,
        List<DrawOrder> drawOrders,
        Timeline timeline)
    {
        var drawOrder = new DrawOrder
        {
            Time = initTime ,
            Offsets = drawOrders.Find(x => Math.Abs(x.Time - initTime) < .01f)?.Offsets ?? []
        };
        
        //var boundingBoxVertices = new[]{0,0, float.MaxValue,float.MaxValue, 0,0, float.MinValue,float.MinValue};
        for (var index = 0; index < layers.Count; index++)
        {
            AddAnimationAttachments(keyframeLayerNames, layers[index].LayerName, spineAnimation, initTime);

            AddAnimationVertices(initTime, deform, layers[index], timeline);

            AddDrawOrderOffset(layers[index].LayerName, index, drawOrder);
            //GetBoundingBoxVertices(layers[index],ref boundingBoxVertices);
        }

        // Order By Slot
        drawOrder.Offsets = drawOrder.Offsets.OrderBy(x => x.SlotNum).ToList();
        drawOrders.Add(drawOrder);
        IsResetOffsetList.Clear();
        // drawOrders.Add(new DrawOrder
        // {
        //     Time = initTime + timeline.Time * Fps, Offsets = drawOrder.Offsets
        // });
        RemoveNotExistsLayer(keyframeLayerNames, layers, spineAnimation, initTime);

        //SetBoundingBoxVertices(initTime, deform, boundingBoxVertices);
    }

    private void GetBoundingBoxVertices(KeyframeLayer layer, ref float[] boundingBoxVertices)
    {
        var minAndMaxSrcPoints = ProcessUtility.FindMinAndMaxPoints(layer.Dstquad).ToList();
        minAndMaxSrcPoints.AddRange(boundingBoxVertices);
        var minAndMaxPoints = ProcessUtility.FindMinAndMaxPoints(minAndMaxSrcPoints.ToArray());
        var width = minAndMaxPoints[2] - minAndMaxPoints[0];
        var height = minAndMaxPoints[3] - minAndMaxPoints[1];
        boundingBoxVertices =
        [
            width + minAndMaxPoints[0], 0,
            minAndMaxPoints[0], minAndMaxPoints[1],
            0, 0,
            minAndMaxPoints[2], minAndMaxPoints[3]
        ];
    }

    private void SetBoundingBoxVertices(float time, Deform deform, float[] boundingBoxVertices)
    {
        deform.SkinName["default"]["boundingbox"].Name = "boundingbox";
        deform.SkinName["default"]["boundingbox"].ImageVertices.Add(new AnimationVertices
        {
            Time = time,
            Vertices = boundingBoxVertices,
        });
    }

    /// <summary>
    /// Remove useless last keyframe layers, keeping next keyframe needs layers.
    /// </summary>
    private void RemoveNotExistsLayer(HashSet<string> keyframeLayerNames,
        List<KeyframeLayer> layers,
        SpineAnimation spineAnimation,
        float initTime)
    {
        //remove layers if they are not display next time
        var notContainsLayers = keyframeLayerNames
            .Where(x => !layers.Exists(y => y.LayerName.Equals(x)));
        foreach (var layerName in notContainsLayers)
        {
            spineAnimation.Slots[layerName].Attachment
                .Add(new AnimationAttachment
                {
                    Time = initTime,
                    Name = null
                });
            keyframeLayerNames.Remove(layerName);
        }
    }

    private void AddAnimationVertices(float initTime, Deform deform, KeyframeLayer layer, Timeline timeline)
    {
        AnimationVertices item = new() { Time = initTime };
        var skinName = _processedImageData.LayerDataDict[layer.LayerName].SkinName;

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

        InterpolateAnimation(layer, timeline, value, item);
    }

    private void InterpolateAnimation(KeyframeLayer layer, Timeline timeline, AnimationDefault value,
        AnimationVertices item)
    {
        LineInterpolateAnimation(timeline, layer, value, item);

        //if (MixAnimationMatrix(initTime, layer, timeline, value)) return;

        SteppedInterpolateAnimation(layer, timeline, value, item);
    }

    /// <summary>
    /// Stepped animation
    /// </summary>
    private void SteppedInterpolateAnimation(KeyframeLayer layer, Timeline timeline, AnimationDefault value,
        AnimationVertices item)
    {
        //var vert = AnimationMatrixUtility.QuadMultiply(timeline.AnimationMatrix, layer.DstMatrix);
        //vert = AnimationMatrixUtility.GetPerspectiveQuad(vert);
        if (!timeline.IsKeyframeMix)
        {
            value.ImageVertices.Add(new AnimationVertices
            {
                Time = item.Time + timeline.Time / 60f,
                Vertices = ProcessUtility.MinusFloats(layer.Dstquad, layer.ZeroCenterPoints),
                //Vertices = layer.Dstquad,
            });
        }
    }

    /// <summary>
    /// AnimationMatrixMix, interpolated by gave data, if step next can not continue
    /// </summary>
    private bool MixAnimationMatrix(float initTime, KeyframeLayer layer, Timeline timeline, AnimationDefault value)
    {
        if (!timeline.IsMatrixMix || timeline.Next is null)
        {
            return false;
        }

        var dstMatrix = timeline.Next.AnimationMatrix;
        var srcMatrix = timeline.AnimationMatrix;
        var curTime = initTime;
        for (var i = 1; i < timeline.Time; i++)
        {
            curTime += Fps * i;
            var rate = (float)i / timeline.Time;
            var vert = AnimationMatrixUtility.QuadMultiply(Matrix.Lerp(srcMatrix, dstMatrix, rate), layer.DstMatrix);
            //vert = AnimationMatrixUtility.GetPerspectiveQuad(vert);
            value.ImageVertices.Add(new AnimationVertices
            {
                Time = curTime,
                Vertices = ProcessUtility.MinusFloats(vert.ToFloats(), layer.ZeroCenterPoints),
                //Vertices = vert.ToFloats(),
            });
        }

        return true;
    }

    /// <summary>
    /// Line interpolated by software, if step next can not continue
    /// </summary>
    private void LineInterpolateAnimation(Timeline timeline, KeyframeLayer layer, AnimationDefault value,
        AnimationVertices item)
    {
        //var vert = AnimationMatrixUtility.QuadMultiply(timeline.AnimationMatrix, layer.DstMatrix);
        //vert = AnimationMatrixUtility.GetPerspectiveQuad(vert);
        //item.Vertices = layer.Dstquad;
        item.Vertices = ProcessUtility.MinusFloats(layer.Dstquad, layer.ZeroCenterPoints);
        value.ImageVertices.Add(item);
    }

    private void AddAnimationAttachments(HashSet<string> keyframeLayerNames, string layerName,
        SpineAnimation spineAnimation,
        float initTime)
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
            Time = initTime,
            Name = layerName
        });
    }

    private List<int> IsResetOffsetList = new(50);
    private void AddDrawOrderOffset(string layerName, int index, DrawOrder drawOrder)
    {
        var slotOrder = _spineJson.FrozenSlotsDict[layerName].Order;
        var offset = index - slotOrder;
        
        if (offset >= 0)
        {
            if(slotOrder - IsResetOffsetList.Count == 0) offset = 0;
            IsResetOffsetList.Add(1);
        }

        if (offset < 0 && (index == 0 || IsResetOffsetList[^1] != 0))
        {
            IsResetOffsetList.Add(0);
        }
        
        if (offset == 0) return;
        
        drawOrder.Offsets.Add(new DrawOrderOffset
        {
            Slot = layerName,
            Offset = offset,
            SlotNum = slotOrder
        });
    }
}