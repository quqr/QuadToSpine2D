using System.Collections.Concurrent;
using System.Collections.Frozen;
using QuadToSpine2D.Core.Data.Spine;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core.Process;

public class ProcessSpineJson
{
    private const    float Fps = 60f;
    private readonly Skin _hitboxSkin = new() { Name = "Hitbox", Attachments = [] };
    private readonly ConcurrentDictionary<string, SpineAnimation> _spineAnimations = [];
    private readonly SpineJsonData _spineJsonData = new();
    private          ProcessImage _processedImageData = null!;

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
        _processedImageData                      = processImage;
        _spineJsonData.SpineSkeletons.ImagesPath = GlobalData.ImageSavePath;
        _spineJsonData.Bones.Add(new SpineBone { Name = "root" });

        InitImageSlot(processImage);
        InitHitboxSlot(quadJsonData);

        SortSlots();

        _spineJsonData.Skins.Add(_hitboxSkin);
        _spineJsonData.FrozenSlotsDict = _spineJsonData.SlotsDict.ToFrozenDictionary();
    }

    private void InitHitboxSlot(QuadJsonData quadJsonData)
    {
        foreach (var hitbox in quadJsonData.Hitbox)
            for (var i = 0; i < hitbox.Layer.Count; i++)
            {
                var hitboxLayerName = $"{hitbox.Name}_{i}";
                hitbox.Layer[i].Name = hitboxLayerName;
                _spineJsonData.Slots.Add(new SpineSlot
                {
                    Name       = hitboxLayerName,
                    Attachment = "boundingbox",
                    OrderId    = int.MaxValue
                });
                _hitboxSkin.Attachments.Add(new Attachments
                {
                    Mesh = new Boundingbox
                    {
                        Name = hitboxLayerName
                    }
                });
            }
    }

    private void InitImageSlot(ProcessImage processImage)
    {
        var imageData = processImage.ImageData;

        for (var skinIndex = 0; skinIndex < imageData.Count; skinIndex++)
            foreach (var (texIdIndex, layersData) in imageData[skinIndex])
            {
                _spineJsonData.Skins.Add(new Skin { Name = $"tex_id_{texIdIndex}/skin_{skinIndex}" });
                for (var i = 0; i < layersData.Count; i++)
                    InitBaseData(layersData.ElementAt(i).Value, skinIndex,
                        texIdIndex, i);
            }
    }

    private void SortSlots()
    {
        _spineJsonData.Slots.Sort((x, y) => x.OrderId.CompareTo(y.OrderId));
        for (var index = 0; index < _spineJsonData.Slots.Count; index++) _spineJsonData.Slots[index].Order = index;
    }


    private void InitBaseData(LayerData layerData, int skinIndex, int texIdIndex, int guidIndex)
    {
        var slotName = layerData.ImageName;
        layerData.SkinName = _spineJsonData.Skins.Last().Name;
        var spineSlot = new SpineSlot
        {
            Name    = slotName, Attachment = slotName,
            OrderId = layerData.KeyframeLayer.OrderId
        };
        _spineJsonData.Slots.Add(spineSlot);
        _spineJsonData.SlotsDict[slotName] = spineSlot;

        //the first is mesh and it has animations
        if (TryInitBaseMesh(layerData, skinIndex, texIdIndex, slotName)) return;

        //the linked mesh base on first mesh and animations also base on it
        InitLinkedMesh(texIdIndex, guidIndex, slotName);
    }

    private void InitLinkedMesh(int texIdIndex, int guidIndex, string slotName)
    {
        _spineJsonData.Skins.Last().Attachments.Add(new Attachments
        {
            Mesh = new LinkedMesh
            {
                Name = slotName,
                Type = "linkedmesh",
                Skin = $"tex_id_{texIdIndex}/skin_0",
                Parent = _spineJsonData.Skins[texIdIndex]
                                       .Attachments[guidIndex].Mesh.Name
            }
        });
    }

    private bool TryInitBaseMesh(LayerData layerData, int skinIndex, int texIdIndex,
        string                             slotName)
    {
        if (skinIndex != 0) return false;
        _spineJsonData.Skins[texIdIndex].Attachments.Add(new Attachments
        {
            Mesh = new Mesh
            {
                Name = slotName,
                Uvs  = layerData.KeyframeLayer.UVs,
                Vertices = layerData.KeyframeLayer
                                    .ZeroCenterPoints
            }
        });
        return true;
    }

    private void ProcessAnimation(QuadJsonData quad)
    {
#if DEBUG
        foreach (var skeleton in quad.Skeleton) SetKeyframesData(quad, skeleton);
#endif

#if RELEASE
        Parallel.ForEach(quad.Skeleton, skeleton => { SetKeyframesData(quad, skeleton); });
#endif
        _spineJsonData.Animations = _spineAnimations.ToDictionary();
    }

    private void SetKeyframesData(QuadJsonData quad, QuadSkeleton skeleton)
    {
        List<Animation> animations     = [];
        List<DrawOrder> drawOrders     = [];
        SpineAnimation  spineAnimation = new();
        Deform          deform         = new();
        animations
           .AddRange(skeleton.Bone
                             .Select(bone => quad.Animation
                                                 .First(x => x.Id == bone.Attach.Id)));
        ProcessAnimations(animations, spineAnimation, deform, drawOrders);

        SetAnimationData(skeleton, spineAnimation, deform, animations);
    }

    private void ProcessAnimations(
        List<Animation> animations,
        SpineAnimation  spineAnimation,
        Deform          deform,
        List<DrawOrder> drawOrders)
    {
        // Combine animations
        foreach (var animation in animations)
        {
            var allKeyframeLayerNames = new HashSet<string>();
            var allHitboxLayerNames   = new HashSet<string>();
            var time                  = 0f;
            foreach (var timeline in animation.Timeline)
            {
                if (timeline.Attach is not null)
                {
                    var keyframe = GetKeyframe(timeline);
                    if (keyframe?.Layers is not null)
                        AddKeyframe(keyframe, time, allKeyframeLayerNames, spineAnimation, deform, drawOrders,
                            timeline);

                    var hitbox = GetHitBox(timeline);
                    if (hitbox is not null)
                        AddHitbox(hitbox, time, allHitboxLayerNames, deform, spineAnimation);
                }

                // FPS : 60
                time += (timeline.Frames + 1) / Fps;
            }
        }

        SortDrawOrderAsync(spineAnimation, drawOrders);
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
        foreach (var drawOrder in drawOrders) drawOrder.SortOffset();

        drawOrders.RemoveAll(x => x.Offsets.Count == 0);
        spineAnimation.DrawOrder = drawOrders.Count != 0 ? drawOrders : null;
        drawOrders.Sort((x, y) => x.Time.CompareTo(y.Time));
#endif
    }


    private void AddHitbox(Hitbox hitbox, float time, HashSet<string> allHitboxLayerNames, Deform deform,
        SpineAnimation            spineAnimation)
    {
        var currentHitboxLayerNames = new List<string>();
        foreach (var hitboxLayer in hitbox.Layer)
        {
            currentHitboxLayerNames.Add(hitboxLayer.Name);
            var vert  = hitboxLayer.Hitquad;
            var value = GetAnimationDefaultValue(deform, hitboxLayer.Name, "Hitbox");

            AddHitboxLayerVertices(time, value, vert);
            AddLayerAttachments(allHitboxLayerNames, hitboxLayer.Name, spineAnimation, time);
        }

        RemoveExtraLayers(allHitboxLayerNames, currentHitboxLayerNames, spineAnimation, time);
    }

    private static void AddHitboxLayerVertices(float time, AnimationDefault value, float[] vert)
    {
        value.ImageVertices.Add(new AnimationVertices
        {
            Time     = time,
            Vertices = vert
        });
    }

    private Hitbox? GetHitBox(Timeline timeline)
    {
        return timeline.Attach?.AttachType switch
        {
            AttachType.HitBox => timeline.Attach.Hitbox,
            _                 => null
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
        QuadSkeleton    skeleton,
        SpineAnimation  spineAnimation,
        Deform          deform,
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
        Keyframe        keyframe,
        float           initTime,
        HashSet<string> keyframeLayerNames,
        SpineAnimation  spineAnimation,
        Deform          deform,
        List<DrawOrder> drawOrders,
        Timeline        timeline)
    {
        var                    layers          = keyframe.Layers;
        var                    drawOrder       = new DrawOrder { Time = initTime };
        var                    existDrawOrder  = drawOrders.Find(x => Math.Abs(x.Time - initTime) < .01f);
        DrawOrder.LayerOffset? lastLayerOffset = null;
        // existDrawOrder may have same layers,  and it is an error.
        // Maybe the process image have bugs, which just process a layer, not a full track. 
        if (existDrawOrder is not null)
        {
            drawOrder       = existDrawOrder;
            lastLayerOffset = drawOrder.LayerOffsets.Last();
        }

        var currentKeyframeLayerNames = new List<string>();
        for (var index = 0; index < layers.Count; index++)
        {
            currentKeyframeLayerNames.Add(layers[index].LayerName);

            AddLayerAttachments(keyframeLayerNames, layers[index].LayerName, spineAnimation, initTime);

            AddLayerVertices(initTime, deform, layers[index], timeline);

            AddLayerOffsets(drawOrder, layers, index, existDrawOrder, lastLayerOffset);
        }

        if (existDrawOrder is null) drawOrders.Add(drawOrder);

        RemoveExtraLayers(keyframeLayerNames, currentKeyframeLayerNames, spineAnimation, initTime);
    }

    private IEnumerable<string> FindNotContainsLayers(HashSet<string> allLayerNames, List<string> currentLayerNames)
    {
        //remove layers if they are not display next time
        return allLayerNames.Except(currentLayerNames);
    }

    private void AddLayerOffsets(
        DrawOrder              drawOrder,
        List<KeyframeLayer?>   layers,
        int                    index,
        DrawOrder?             existDrawOrder,
        DrawOrder.LayerOffset? lastLayerOffset)
    {
        var existLayerOffset = existDrawOrder?.LayerOffsets
                                              .Find(x => x.LayerName.Equals(layers[index].LayerName));
        if (existLayerOffset is not null)
        {
            Console.WriteLine($"{layers[index].LayerName} already exist in drawOrder");
            return;
        }

        drawOrder.LayerOffsets.Add(new DrawOrder.LayerOffset
        {
            LayerName      = layers[index].LayerName,
            LayerSlotOrder = _spineJsonData.FrozenSlotsDict[layers[index].LayerName].Order,
            LayerIndex = existDrawOrder is null
                             ? index
                             : lastLayerOffset.LayerIndex + index + 1
        });
    }


    /// <summary>
    ///     Remove useless last keyframe layers, keeping next keyframe needs layers.
    /// </summary>
    private void RemoveExtraLayers(
        HashSet<string> allLayerNames,
        List<string>    currentLayerNames,
        SpineAnimation  spineAnimation,
        float           initTime)
    {
        var notContainsLayers = FindNotContainsLayers(allLayerNames, currentLayerNames);
        foreach (var layerName in notContainsLayers)
        {
            allLayerNames.Remove(layerName);
            var slot = spineAnimation.Slots[layerName];
            if (slot.Attachment.Last().Name is null) continue;
            slot.Attachment.Add(new AnimationAttachment
            {
                Time = initTime,
                Name = null
            });
        }
    }

    private void AddLayerVertices(float initTime, Deform deform, KeyframeLayer layer,
        Timeline                        timeline)
    {
        AnimationVertices item = new() { Time = initTime };

        var value = GetAnimationDefaultValue(deform, layer.LayerName,
            _processedImageData.LayerDataDict[layer.LayerName].SkinName);

        InterpolateAnimation(layer, timeline, value, item, initTime);

        value.ImageVertices.Sort((x, y) => x.Time.CompareTo(y.Time));
    }

    private AnimationDefault GetAnimationDefaultValue(Deform deform, string valueName, string skinName)
    {
        if (!deform.SkinName.ContainsKey(skinName))
            deform.SkinName[skinName] = [];
        if (!deform.SkinName[skinName].TryGetValue(valueName, out var value))
        {
            value                                = new AnimationDefault { Name = valueName };
            deform.SkinName[skinName][valueName] = value;
        }

        return value;
    }

    private void InterpolateAnimation(KeyframeLayer layer, Timeline timeline, AnimationDefault value,
        AnimationVertices                           item,  float    initTime)
    {
        // if (timeline.IsMatrixMix)
        // {
        //     MixAnimationMatrix(initTime, layer, timeline, value);
        //     return;
        // }

        LineInterpolateAnimation(layer, value, item, timeline);

        if (!timeline.IsKeyframeMix) SteppedInterpolateAnimation(timeline, value, item);
    }

    /// <summary>
    ///     Stepped animation
    /// </summary>
    private void SteppedInterpolateAnimation(Timeline timeline, AnimationDefault value,
        AnimationVertices                             item)
    {
        value.ImageVertices.Add(new AnimationVertices
        {
            Time     = item.Time + timeline.Frames / Fps,
            Vertices = value.ImageVertices.Last().Vertices
        });
    }

    /// <summary>
    ///     AnimationMatrixMix, interpolated by gave data, if step next can not continue
    /// </summary>
    private void MixAnimationMatrix(float initTime, KeyframeLayer layer, Timeline timeline, AnimationDefault value)
    {
        var srcMatrix = timeline.AnimationMatrix;
        var dstMatrix = timeline.Next?.AnimationMatrix ?? srcMatrix;
        for (var i = 0; i < timeline.Frames; i++)
        {
            var rate = i / timeline.Frames;
            var vert = AnimationMatrixUtility.QuadMultiply(Matrix.Lerp(srcMatrix, dstMatrix, rate), layer.DstMatrix);
            value.ImageVertices.Add(new AnimationVertices
            {
                Time     = initTime + i / Fps,
                Vertices = ProcessUtility.MinusFloats(vert.ToFloats(), layer.ZeroCenterPoints)
            });
        }
    }

    /// <summary>
    ///     Line interpolated by software, if step next can not continue
    /// </summary>
    private void LineInterpolateAnimation(KeyframeLayer layer, AnimationDefault value,
        AnimationVertices                               item,  Timeline         timeline)
    {
        var vert = AnimationMatrixUtility.QuadMultiply(timeline.AnimationMatrix, layer.DstMatrix);
        // Make sure the image to center
        item.Vertices = ProcessUtility.MinusFloats(vert.ToFloats(), layer.ZeroCenterPoints);
        value.ImageVertices.Add(item);
    }

    private void AddLayerAttachments(HashSet<string> layerNames, string layerName,
        SpineAnimation                               spineAnimation,
        float                                        initTime)
    {
        // It is new or be deleted if success
        if (!layerNames.Add(layerName)) return;
        // Init slot
        if (!spineAnimation.Slots.TryGetValue(layerName, out var value))
        {
            value                           = new AnimationSlot();
            spineAnimation.Slots[layerName] = value;
        }

        value.Attachment.Add(new AnimationAttachment
        {
            Time = initTime,
            Name = layerName
        });
        value.Attachment.Sort((x, y) => x.Time.CompareTo(y.Time));
    }
}