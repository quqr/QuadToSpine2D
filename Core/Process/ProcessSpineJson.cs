using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Threading.Tasks;
using QuadToSpine2D.Core.Data.Spine;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core.Process;

public class ProcessSpineJson
{
    private readonly SpineJsonData _spineJsonData = new();
    private ProcessImage _processedImageData = null!;
    private readonly ConcurrentDictionary<string, SpineAnimation> _spineAnimations = [];

    public SpineJsonData Process(ProcessImage processImage, QuadJsonData quadJsonData)
    {
        Console.WriteLine("Writing spine json...");
        GlobalData.BarTextContent = "Writing spine json...";

        InitData(processImage, quadJsonData);
        GlobalData.BarValue = 70;
        ProcessAnimation(quadJsonData);
        GlobalData.BarValue = 90;
        return _spineJsonData;
    }

    private void InitData(ProcessImage processImage, QuadJsonData quadJsonData)
    {
        _processedImageData = processImage;
        _spineJsonData.SpineSkeletons.ImagesPath = GlobalData.ImageSavePath;
        _spineJsonData.Bones.Add(new SpineBone { Name = "root" });
        
        Task[] tasks = [InitImageAndBoundingboxSlot(processImage), InitHitboxSlot(quadJsonData)];
        tasks[0].Start();
        tasks[1].Start();
        Task.WaitAll(tasks);
        
        OrderSlots();

        if (GlobalData.IsAddBoundingbox) _spineJsonData.Skins.Add(_boundingboxSkin);
        if (GlobalData.IsAddHitbox) _spineJsonData.Skins.Add(_hitboxSkin);
        _spineJsonData.Skins.Add(_hitboxSkin);
        _spineJsonData.FrozenSlotsDict = _spineJsonData.SlotsDict.ToFrozenDictionary();
    }

    private List<string> _hitboxes = [];
    private Task InitHitboxSlot(QuadJsonData quadJsonData)
    {
        return new Task(() =>
        {
            var maxHitboxLayerCount = quadJsonData.Animation
                .Max(x=>x.Timeline
                    .Sum(y => y.Attach?.Hitbox?.Layer.Count)) ?? 0;
            for (var i = 0; i < maxHitboxLayerCount; i++)
            {
                var hitboxName = $"hitbox_{i}";
                _hitboxes.Add(hitboxName);
                _spineJsonData.Slots.Add(new SpineSlot
                {
                    Name = hitboxName,
                    Bone = "root",
                    Attachment = "boundingbox",
                    OrderId = int.MaxValue,
                });
                _hitboxSkin.Attachments.Add(new Attachments
                {
                    Value = new Boundingbox()
                    {
                        Name = hitboxName,
                        Type = "boundingbox",
                    }
                });
            }
        });
    }

    private Task InitImageAndBoundingboxSlot(ProcessImage processImage)
    {
        return new Task(() =>
        {
            for (var curFullSkinIndex = 0; curFullSkinIndex < processImage.ImageData.Count; curFullSkinIndex++)
            {
                for (var texIdIndex = 0; texIdIndex < processImage.ImageData[curFullSkinIndex].Count; texIdIndex++)
                {
                    var layerNames = processImage.ImageData[curFullSkinIndex][texIdIndex];
                    if (layerNames is null) continue;
                    _spineJsonData.Skins.Add(new Skin { Name = $"tex_id_{texIdIndex}/skin_{curFullSkinIndex}" });
                    for (var layerNameIndex = 0; layerNameIndex < layerNames.Count; layerNameIndex++)
                    {
                        InitBaseData(layerNames.ElementAt(layerNameIndex).Value, curFullSkinIndex, texIdIndex,
                            layerNameIndex);
                    }
                }
            }
        });
    }

    private void OrderSlots()
    {
        _spineJsonData.Slots.Sort((x, y) => x.OrderId.CompareTo(y.OrderId));
        for (var index = 0; index < _spineJsonData.Slots.Count; index++)
        {
            _spineJsonData.Slots[index].Order = index;
        }
    }


    private void InitBaseData(LayerData layerData, int curFullSkin, int texIdIndex, int guidIndex)
    {
        var slotName = layerData.ImageName;
        layerData.SkinName = _spineJsonData.Skins.Last().Name;
        var spineSlot = new SpineSlot
        {
            Name = slotName, Attachment = slotName, Bone = "root",
            OrderId = layerData.KeyframeLayer.OrderId
        };
        _spineJsonData.Slots.Add(spineSlot);
        _spineJsonData.SlotsDict[slotName] = spineSlot;

        //the first is mesh and it has animations
        if (InitBaseMesh(layerData, curFullSkin, texIdIndex, slotName, guidIndex)) return;

        //the linked mesh base on first mesh and animations also base on it
        InitLinkedMesh(texIdIndex, guidIndex, slotName);
    }

    private int _boundingboxCount;
    private readonly Skin _boundingboxSkin = new() { Name = "Boundingbox", Attachments = [] };

    private void InitBoundingBox(int layerCount)
    {
        if (!GlobalData.IsAddBoundingbox || _boundingboxCount > layerCount) return;
        _boundingboxSkin.Attachments.Add(new Attachments
        {
            Value = new Boundingbox
            {
                Name = $"boundingbox_{layerCount}",
                Type = "boundingbox",
                Vertices = [0, 0, 0, 0, 0, 0, 0, 0],
            }
        });

        _spineJsonData.Slots.Add(new SpineSlot
        {
            Name = $"boundingbox_{layerCount}",
            Bone = "root",
            Attachment = "boundingbox",
            OrderId = int.MaxValue,
        });
        _boundingboxCount++;
    }

    private void InitLinkedMesh(int texIdIndex, int guidIndex, string slotName)
    {
        _spineJsonData.Skins.Last().Attachments.Add(new Attachments
        {
            Value = new LinkedMesh
            {
                Name = slotName,
                Type = "linkedmesh",
                Skin = $"tex_id_{texIdIndex}/skin_0",
                Parent = _spineJsonData.Skins[texIdIndex].Attachments[guidIndex].Value.Name,
            }
        });
    }

    private bool InitBaseMesh(LayerData layerData, int curFullSkin, int texIdIndex, string slotName, int guidIndex)
    {
        if (curFullSkin != 0) return false;
        _spineJsonData.Skins[texIdIndex].Attachments.Add(new Attachments
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

    private void ProcessAnimation(QuadJsonData quad)
    {
#if DEBUG
        foreach (var skeleton in quad.Skeleton)
            SetKeyframesData(quad, skeleton);
#endif

#if RELEASE
        Parallel.ForEach(quad.Skeleton, skeleton => { SetKeyframesData(quad, skeleton); });
#endif
        _spineJsonData.Animations = _spineAnimations.ToDictionary();
    }

    private void SetKeyframesData(QuadJsonData quad, QuadSkeleton skeleton)
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
                if (timeline.Attach is null)
                {
                    // time += timeline.Time / 60f;
                    continue;
                }
                var keyframe = GetKeyframe(timeline);
                var hitbox = GetHitBox(timeline);
                
                AddKeyframe(keyframe, time, keyframeLayerNames, spineAnimation, deform, drawOrders, timeline);
                // AddHitbox(hitbox, time, keyframeLayerNames, spineAnimation, deform);
                

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
    
    private readonly Skin _hitboxSkin = new() { Name = "Hitbox", Attachments = [] };
    private void AddHitbox(Hitbox? hitbox, float time, HashSet<string> keyframeLayerNames,
        SpineAnimation spineAnimation, Deform deform)
    {
        if (hitbox is null) return;

        for (var i = 0; i < hitbox.Layer.Count; i++)
        {
            var hitboxName = $"hitbox_{i}";
            var value = GetAnimationDefaultValue(deform, hitboxName, "Hitbox");
            value.ImageVertices.Add(new AnimationVertices
            {
                Time = time,
                Vertices = hitbox.Layer[i].Hitquad,
            });
            AddAnimationAttachments(keyframeLayerNames, hitboxName, spineAnimation, time);
        }
    }

    private Hitbox? GetHitBox(Timeline timeline)
    {
        return timeline.Attach?.AttachType switch
        {
            AttachType.HitBox => timeline.Attach.Hitbox,
            _ => null
        };
    }

    private Keyframe? GetKeyframe(Timeline timeline)
    {
        return timeline.Attach?.AttachType switch
        {
            AttachType.Keyframe =>
                timeline.Attach.Keyframe,
            AttachType.Slot =>
                timeline.Attach.Keyframe,
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
        Keyframe? keyframe,
        float initTime,
        HashSet<string> keyframeLayerNames,
        SpineAnimation spineAnimation,
        Deform deform,
        List<DrawOrder> drawOrders,
        Timeline timeline)
    {
        var layers = keyframe?.Layer;
        if(layers is null) return;
        
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
                LayerSlotOrder = _spineJsonData.FrozenSlotsDict[layers[index].LayerName].Order,
                LayerIndex = existDrawOrder is null ? index : lastLayerOffset.LayerIndex + index + 1,
            });

            //AddDrawOrderOffset(layers[index].LayerName, index, drawOrder);
        }

        // Order By Slot
        // drawOrder.Offsets = drawOrder.Offsets.OrderBy(x => x.SlotNum).ToList();

        if (existDrawOrder is null) drawOrders.Add(drawOrder);
        RemoveNotExistsLayer(keyframeLayerNames,layers,spineAnimation, initTime);
        // _isResetOffsetList.Clear();
    }

    private void SetBoundingBoxVertices(HashSet<string> keyframeLayerNames, SpineAnimation spineAnimation, float time,
        float layerIndex, Deform deform, float[]? boundingboxVertices)
    {
        if (!GlobalData.IsAddBoundingbox || boundingboxVertices is null) return;
        var boundingboxName = $"boundingbox_{layerIndex}";
        var value = GetAnimationDefaultValue(deform, boundingboxName, "Boundingbox");
        value.ImageVertices.Add(new AnimationVertices
        {
            Time = time,
            Vertices = boundingboxVertices,
        });
        AddAnimationAttachments(keyframeLayerNames, boundingboxName, spineAnimation, time);
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
        if (GlobalData.IsAddBoundingbox)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                var boundingboxName = $"boundingbox_{i}";
                if (!keyframeLayerNames.Contains(boundingboxName))
                {
                    notContainsLayers.Add(boundingboxName);
                }
            }
        }
        
        notContainsLayers.AddRange(keyframeLayerNames
            .Where(x => !layers
                .Exists(y => y.LayerName.Equals(x)) && !x.Contains("boundingbox")));
        
        foreach (var layerName in notContainsLayers)
        {
            keyframeLayerNames.Remove(layerName);
            if(!spineAnimation.Slots.TryGetValue(layerName, out var slot)) continue;
            if (slot.Attachment.Last().Name is null) continue;
            slot.Attachment.Add(new AnimationAttachment
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

        var value = GetAnimationDefaultValue(deform, layer.LayerName,
            _processedImageData.LayerDataDict[layer.LayerName].SkinName);

        InterpolateAnimation(keyframeLayerNames,
            spineAnimation, layer, timeline, value, item, layerIndex, deform, initTime);
    }

    private AnimationDefault GetAnimationDefaultValue(Deform deform, string valueName, string skinName)
    {
        if (!deform.SkinName.ContainsKey(skinName))
            deform.SkinName[skinName] = [];
        if (!deform.SkinName[skinName].TryGetValue(valueName, out var value))
        {
            value = new AnimationDefault
            {
                Name = valueName
            };
            deform.SkinName[skinName][valueName] = value;
        }

        return value;
    }

    private void InterpolateAnimation(HashSet<string> keyframeLayerNames,
        SpineAnimation spineAnimation, KeyframeLayer layer, Timeline timeline, AnimationDefault value,
        AnimationVertices item, int layerIndex, Deform deform, float initTime)
    {
        var vert = LineInterpolateAnimation(layer, value, item, timeline);
        var time = initTime;
        SetBoundingBoxVertices(keyframeLayerNames, spineAnimation, time, layerIndex, deform, vert);

        // bugs
        // if (MixAnimationMatrix(initTime, layer, timeline, value)) return;

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
        AnimationVertices item, Timeline timeline)
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
        var slotOrder = _spineJsonData.FrozenSlotsDict[layerName].Order;
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