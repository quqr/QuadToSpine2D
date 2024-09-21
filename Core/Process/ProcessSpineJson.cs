using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Threading.Tasks;
using QuadToSpine2D.Core.Data.Spine;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core.Process;

public class ProcessSpineJson
{
    private readonly SpineJson _spineJson = new();
    private ProcessImage _processedImageData = null!;
    private readonly ConcurrentDictionary<string, SpineAnimation> _spineAnimations = [];

    public SpineJson Process(ProcessImage processImage, QuadJson quadJson)
    {
        Console.WriteLine("Writing spine json...");
        GlobalData.BarTextContent = "Writing spine json...";

        InitData(processImage);
        GlobalData.BarValue = 70;
        ProcessAnimation(quadJson);
        GlobalData.BarValue = 90;
        return _spineJson;
    }
    
    private void InitData(ProcessImage processImage)
    {
        _processedImageData = processImage;
        _spineJson.SpineSkeletons.ImagesPath = GlobalData.ImageSavePath;
        _spineJson.Bones.Add(new SpineBone { Name = "root" });
        for (var curFullSkinIndex = 0; curFullSkinIndex < processImage.ImageData.Count; curFullSkinIndex++)
        {
            for (var texIdIndex = 0; texIdIndex < processImage.ImageData[curFullSkinIndex].Count; texIdIndex++)
            {
                var layerNames = processImage.ImageData[curFullSkinIndex][texIdIndex];
                if (layerNames is null) continue;
                _spineJson.Skins.Add(new Skin { Name = $"tex_id_{texIdIndex}/skin_{curFullSkinIndex}" });
                for (var layerNameIndex = 0; layerNameIndex < layerNames.Count; layerNameIndex++)
                {
                    InitBaseData(layerNames.ElementAt(layerNameIndex).Value, curFullSkinIndex, texIdIndex,
                        layerNameIndex);
                }
            }
        }

        OrderSlots();
        
        if (GlobalData.IsAddBoundingBox) _spineJson.Skins.AddRange(_boundingBoxSkins);
        if (GlobalData.IsAddHitBox) _spineJson.Skins.Add(new Skin { Name = "HitBox", Attachments = [] });
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
        if (InitBaseMesh(layerData, curFullSkin, texIdIndex, slotName, guidIndex)) return;

        //the linked mesh base on first mesh and animations also base on it
        InitLinkedMesh(texIdIndex, guidIndex, slotName);
    }

    private int _boundingBoxCount;
    private readonly List<Skin> _boundingBoxSkins = [new Skin { Name = "BoundingBox", Attachments = [] }];

    private void InitBoundingBox(int layerCount)
    {
        if (!GlobalData.IsAddBoundingBox || _boundingBoxCount > layerCount) return;
        _boundingBoxSkins[0].Attachments.Add(new Attachments
        {
            Value = new BoundingBox
            {
                Name = $"boundingbox_{layerCount}",
                Type = "boundingbox",
                Vertices = [0, 0, 0, 0, 0, 0, 0, 0],
            }
        });

        _spineJson.Slots.Add(new SpineSlot
        {
            Name = $"boundingbox_{layerCount}",
            Bone = "root",
            Attachment = "boundingbox",
            OrderId = int.MaxValue,
        });
        _boundingBoxCount++;
    }

    private void InitLinkedMesh(int texIdIndex, int guidIndex, string slotName)
    {
        _spineJson.Skins.Last().Attachments.Add(new Attachments
        {
            Value = new LinkedMesh
            {
                Name = slotName,
                Type = "linkedmesh",
                Skin = $"tex_id_{texIdIndex}/skin_0",
                Parent = _spineJson.Skins[texIdIndex].Attachments[guidIndex].Value.Name,
            }
        });
    }

    private bool InitBaseMesh(LayerData layerData, int curFullSkin, int texIdIndex, string slotName, int guidIndex)
    {
        if (curFullSkin != 0) return false;
        _spineJson.Skins[texIdIndex].Attachments.Add(new Attachments
        {
            Value = new Mesh
            {
                Name = slotName,
                Uvs = layerData.KeyframeLayer.UVs,
                Vertices = layerData.KeyframeLayer.ZeroCenterPoints,
            }
        });
        // Just once 
        InitBoundingBox(guidIndex);
        return true;
    }

    private void ProcessAnimation(QuadJson quad)
    {
#if DEBUG
        foreach (var skeleton in quad.Skeleton)
            SetKeyframesData(quad, skeleton);
#endif

#if RELEASE
        Parallel.ForEach(quad.Skeleton, skeleton => { SetKeyframesData(quad, skeleton); });
#endif
        _spineJson.Animations = _spineAnimations.ToDictionary();
    }

    private void SetKeyframesData(QuadJson quad, QuadSkeleton skeleton)
    {
        List<Animation> animations = [];
        List<DrawOrder> drawOrders = [];
        SpineAnimation spineAnimation = new();
        Deform deform = new();
        animations
            .AddRange(skeleton.Bone
                .Select(bone => quad.Animation
                    .First(x => x.Id == bone.Attach.Id)));

        ProcessAnimations(animations, spineAnimation, deform, drawOrders);

        SetAnimationData(skeleton, spineAnimation, deform, animations);
    }


    private void ProcessAnimations(
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
                var layers = GetKeyframeLayers(timeline);
                var hitBoxes = GetHitBoxes(timeline);
                if (layers is not null)
                    AddKeyframe(layers, time, keyframeLayerNames, spineAnimation, deform, drawOrders, timeline);
                else if (hitBoxes is not null)
                    AddHitBox(hitBoxes, time, keyframeLayerNames, spineAnimation, deform, drawOrders, timeline);
                // FPS : 60
                time += (timeline.Time + 1) / 60f;

            }
        }

        // SortDrawOrderAsync(spineAnimation, drawOrders);
    }

    private void SortDrawOrderAsync(SpineAnimation spineAnimation, List<DrawOrder> drawOrders)
    {
    #if RELEASE
        Task.Run(() =>
        {
            foreach (var drawOrder in drawOrders)
            {
                drawOrder.SortOffset();
            }
            drawOrders.RemoveAll(x => x.Offsets.Count == 0);
            spineAnimation.DrawOrder = drawOrders.Count != 0 ? drawOrders : null;
            drawOrders.Sort((x, y) => x.Time.CompareTo(y.Time));
        });
    #endif
    #if DEBUG
        foreach (var drawOrder in drawOrders)
        {
            drawOrder.SortOffset();
        }
        drawOrders.RemoveAll(x => x.Offsets.Count == 0);
        spineAnimation.DrawOrder = drawOrders.Count != 0 ? drawOrders : null;
        drawOrders.Sort((x, y) => x.Time.CompareTo(y.Time));
    #endif
    }

    private void AddHitBox(List<HitboxLayer> hitBoxes, float time, HashSet<string> keyframeLayerNames,
        SpineAnimation spineAnimation, Deform deform, List<DrawOrder> drawOrders, Timeline timeline)
    {
        // TODO : implement hitbox animation
    }

    private List<HitboxLayer>? GetHitBoxes(Timeline timeline)
    {
        return timeline.Attach?.AttachType switch
        {
            AttachType.HitBox => timeline.Attach.Hitbox?.Layer,
            _ => null
        };
    }

    private List<KeyframeLayer>? GetKeyframeLayers(Timeline timeline)
    {
        return timeline.Attach?.AttachType switch
        {
            AttachType.Keyframe =>
                timeline.Attach.Keyframe?.Layer!,
            AttachType.Slot =>
                timeline.Attach.Keyframe?.Layer!,
            _ => null
        };
    }

    private void SetAnimationData(
        QuadSkeleton skeleton,
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

        spineAnimation.Deform = deform;

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
        var drawOrder = new DrawOrder() { Time = initTime };
        var existDrawOrder = drawOrders.Find(x => Math.Abs(x.Time - initTime) < .01f);
        DrawOrder.LayerOffset? lastLayerOffset = null;
        if (existDrawOrder is not null)
        {
            drawOrder = existDrawOrder;
            lastLayerOffset = drawOrder.LayerOffsets.Last();
        }

        for (var index = 0; index < layers.Count; index++)
        {
            AddAnimationAttachments(keyframeLayerNames, layers[index].LayerName, spineAnimation, initTime);

            AddAnimationVertices(keyframeLayerNames, spineAnimation, initTime, deform, layers[index], index, timeline);

            drawOrder.LayerOffsets.Add(new DrawOrder.LayerOffset
            {
                LayerName = layers[index].LayerName,
                LayerSlotOrder = _spineJson.FrozenSlotsDict[layers[index].LayerName].Order,
                LayerIndex = existDrawOrder is null ? index : lastLayerOffset.LayerIndex + index + 1,
            });

            //AddDrawOrderOffset(layers[index].LayerName, index, drawOrder);
        }

        // Order By Slot
        // drawOrder.Offsets = drawOrder.Offsets.OrderBy(x => x.SlotNum).ToList();

        if (existDrawOrder is null) drawOrders.Add(drawOrder);

        // _isResetOffsetList.Clear();

        RemoveNotExistsLayer(keyframeLayerNames, layers, spineAnimation, initTime);
    }

    private void SetBoundingBoxVertices(HashSet<string> keyframeLayerNames, SpineAnimation spineAnimation, float time,
        float layerIndex, Deform deform, float[]? boundingBoxVertices)
    {
        if (!GlobalData.IsAddBoundingBox || boundingBoxVertices is null) return;
        var boundingBoxName = $"boundingbox_{layerIndex}";
        if (!deform.SkinName.ContainsKey("BoundingBox"))
            deform.SkinName["BoundingBox"] = [];
        if (!deform.SkinName["BoundingBox"].TryGetValue(boundingBoxName, out var value))
        {
            value = new AnimationDefault
            {
                Name = boundingBoxName
            };
            deform.SkinName["BoundingBox"][boundingBoxName] = value;
        }

        value.ImageVertices.Add(new AnimationVertices
        {
            Time = time,
            Vertices = boundingBoxVertices,
        });
        AddAnimationAttachments(keyframeLayerNames, boundingBoxName, spineAnimation, time);
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
        var notContainsLayers = new List<string>();
        if (GlobalData.IsAddBoundingBox)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                var boundingBoxName = $"boundingbox_{i}";
                if (!keyframeLayerNames.Contains(boundingBoxName))
                {
                    notContainsLayers.Add(boundingBoxName);
                }
            }
        }

        notContainsLayers.AddRange(keyframeLayerNames
            .Where(x => !layers
                .Exists(y => y.LayerName.Equals(x)) && !x.Contains("boundingbox")));

        foreach (var layerName in notContainsLayers)
        {
            keyframeLayerNames.Remove(layerName);
            var attachment = spineAnimation.Slots[layerName].Attachment;
            if (attachment.Last().Name is null) return;
            attachment.Add(new AnimationAttachment
            {
                Time = initTime,
                Name = null
            });
        }
    }

    private void AddAnimationVertices(HashSet<string> keyframeLayerNames,
        SpineAnimation spineAnimation, float initTime, Deform deform, KeyframeLayer layer, int layerIndex,
        Timeline timeline)
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

        InterpolateAnimation(keyframeLayerNames,
            spineAnimation, layer, timeline, value, item, layerIndex, deform, initTime);
    }

    private void InterpolateAnimation(HashSet<string> keyframeLayerNames,
        SpineAnimation spineAnimation, KeyframeLayer layer, Timeline timeline, AnimationDefault value,
        AnimationVertices item, int layerIndex, Deform deform, float initTime)
    {
        var vert = LineInterpolateAnimation(layer, value, item,timeline);
        var time = initTime;
        SetBoundingBoxVertices(keyframeLayerNames, spineAnimation, time, layerIndex, deform, vert);

        // bugs
        //if (MixAnimationMatrix(initTime, layer, timeline, value)) return;

        SteppedInterpolateAnimation(layer, timeline, value, item);
        time = initTime + timeline.Time / 60f;
        SetBoundingBoxVertices(keyframeLayerNames, spineAnimation, time, layerIndex, deform, vert);
    }

    /// <summary>
    /// Stepped animation
    /// </summary>
    private void SteppedInterpolateAnimation(KeyframeLayer layer, Timeline timeline, AnimationDefault value,
        AnimationVertices item)
    {
        value.ImageVertices.Add(new AnimationVertices
        {
            Time = item.Time + timeline.Time / 60f,
            Vertices = value.ImageVertices.Last().Vertices,
        });
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
        for (var i = 1; i < timeline.Time; i++)
        {
            var rate = i / timeline.Time;
            var vert = AnimationMatrixUtility.QuadMultiply(Matrix.Lerp(srcMatrix, dstMatrix, rate), layer.DstMatrix);
            value.ImageVertices.Add(new AnimationVertices
            {
                Time = initTime + i / 60f,
                Vertices = ProcessUtility.MinusFloats(vert.ToFloats(), layer.ZeroCenterPoints),
            });
        }

        return true;
    }

    /// <summary>
    /// Line interpolated by software, if step next can not continue
    /// </summary>
    private float[] LineInterpolateAnimation(KeyframeLayer layer, AnimationDefault value,
        AnimationVertices item,Timeline timeline)
    {
        var vert = AnimationMatrixUtility.QuadMultiply(timeline.AnimationMatrix, layer.DstMatrix);
        // Make sure the image to center
        item.Vertices = ProcessUtility.MinusFloats(vert.ToFloats(), layer.ZeroCenterPoints);
        value.ImageVertices.Add(item);
        return item.Vertices;
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

    private readonly List<bool> _isResetOffsetList = [];

    private void AddDrawOrderOffset(string layerName, int index, DrawOrder drawOrder)
    {
        var slotOrder = _spineJson.FrozenSlotsDict[layerName].Order;
        var offset = index - slotOrder;

        if (offset >= 0)
        {
            if (slotOrder - _isResetOffsetList.Count == 0) offset = 0;
            _isResetOffsetList.Add(true);
            if (offset == 0) return;
        }
        else if (index == 0 || _isResetOffsetList[^1])
        {
            _isResetOffsetList.Add(false);
        }

        drawOrder.Offsets.Add(new DrawOrderOffset
        {
            Slot = layerName,
            Offset = offset,
            SlotNum = slotOrder
        });
    }
}