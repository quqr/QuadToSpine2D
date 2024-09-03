using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Threading.Tasks;
using QuadToSpine2D.Core.Data.Spine;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core.Process;

public class ProcessSpineJson
{
    private readonly SpineJson _spineJson;
    private ProcessImage _processedImageData;
    private readonly ConcurrentDictionary<string, SpineAnimation> _spineAnimations;
    private int _curSlotIndex;
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
        var spineSlot = new SpineSlot { Name = slotName, Attachment = slotName, Bone = "root", Order = _curSlotIndex };
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
                    Uvs = layerData.UVs,
                    Vertices = layerData.ZeroCenterPoints,
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
        // foreach (var skeleton in quad.Skeleton)
        //     SetKeyframesData(quad, skeleton);
        Parallel.ForEach(quad.Skeleton, skeleton => { SetKeyframesData(quad, skeleton); });
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
        for (var index = 0; index < animations.Count; index++)
        {
            // Combine animation tracks? Not now !
            if (index >= 1) break;
            var animation = animations[index];
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
                Console.WriteLine($"Can Not Process Animation Attach Type : {timeline.Attach.AttachType}");
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
        var drawOrder = new DrawOrder { Time = initTime };
        for (var index = 0; index < layers.Count; index++)
        {
            AddAnimationAttachments(keyframeLayerNames, layers[index].LayerName, spineAnimation, initTime);

            AddAnimationVertices(initTime, deform, layers[index], timeline);

            AddDrawOrderOffset(layers[index].LayerName, index, drawOrder);
        }

        // Order By Slot
        drawOrder.Offsets = drawOrder.Offsets.OrderBy(x => x.SlotNum).ToList();
        drawOrders.Add(drawOrder);

        RemoveNotExistsLayer(keyframeLayerNames, layers, spineAnimation, initTime);
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

        InterpolateAnimation(initTime, layer, timeline, value, item);
    }

    private void InterpolateAnimation(float initTime, KeyframeLayer layer, Timeline timeline, AnimationDefault value,
        AnimationVertices item)
    {
        LineInterpolateAnimation(layer, value, item);

        if (MixAnimationMatrix(initTime, layer, timeline, value)) return;

        SteppedInterpolateAnimation(timeline, value, item);
    }

    /// <summary>
    /// Stepped animation
    /// </summary>
    private void SteppedInterpolateAnimation(Timeline timeline, AnimationDefault value, AnimationVertices item)
    {
        if (!timeline.IsKeyframeMix)
        {
            value.ImageVertices.Add(new AnimationVertices
            {
                Time = item.Time + timeline.Time / 60f,
                Vertices = item.Vertices
            });
        }
    }

    /// <summary>
    /// AnimationMatrixMix, interpolated by gave data, if step next can not continue
    /// </summary>
    private bool MixAnimationMatrix(float initTime, KeyframeLayer layer, Timeline timeline, AnimationDefault value)
    {
        if (timeline is not { IsMatrixMix: true, Next: not null }) return false;
        var dstMatrix = timeline.Next.AnimationMatrix;
        var srcMatrix = timeline.AnimationMatrix;
        var curTime = initTime;
        for (var i = 1; i < timeline.Time; i++)
        {
            curTime += Fps * i;
            var rate = (float)i / timeline.Time;
            var curVert = srcMatrix * (1 - rate) + dstMatrix * rate;
            curVert *= layer.DstMatrix;
            value.ImageVertices.Add(new AnimationVertices
            {
                Time = curTime,
                Vertices = ProcessUtility.MinusFloats(curVert.ToFloats(), layer.ZeroCenterPoints),
            });
        }

        return true;
    }

    /// <summary>
    /// Line interpolated by software, if step next can not continue
    /// </summary>
    private void LineInterpolateAnimation(KeyframeLayer layer, AnimationDefault value, AnimationVertices item)
    {
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