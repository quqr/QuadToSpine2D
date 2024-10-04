using System.Collections.Concurrent;
using System.Threading.Tasks;
using QuadToSpine2D.Core.Data.Spine;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core.Process;

public class NewProcessSpine2DJson
{
    private const    float         Fps         = 1 / 60f;
    private readonly Skin          _hitboxSkin = new() { Name = "Hitbox", Attachments = [] };
    private readonly Pool          _pool       = new();
    private readonly QuadJsonData  _quadJsonData;
    private readonly SpineJsonData _spineJsonData = new();

    public NewProcessSpine2DJson(QuadJsonData quadJsonData)
    {
        GlobalData.BarTextContent                = "Processing...";
        _quadJsonData                            = quadJsonData;
        _spineJsonData.SpineSkeletons.ImagesPath = GlobalData.ImageSavePath;
        _spineJsonData.Bones.Add(new SpineBone { Name = "root" });
    }

    public SpineJsonData Process()
    {
        InitHitboxSlot(_quadJsonData);
        var bar = 65f / _quadJsonData.Skeleton.Count;
        foreach (var skeleton in _quadJsonData.Skeleton)
        {
            GlobalData.BarTextContent = $"Processing animation : {skeleton.Name}";
            SetAnimation(skeleton);
            GlobalData.BarValue += bar;
        }
        SortSlots();
        foreach (var skeleton in _quadJsonData.Skeleton)
        {
            var animation = _spineJsonData.Animations[skeleton.Name];
            SortDrawOrderAsync(animation, animation.DrawOrder);
        }
        return _spineJsonData;
    }
    private void SortSlots()
    {
        _spineJsonData.Slots.Sort((x, y) => x.OrderByImageSlot.CompareTo(y.OrderByImageSlot));
        for (var index = 0; index < _spineJsonData.Slots.Count; index++) 
            _spineJsonData.Slots[index].SlotOrder = index;
    }

    private int _currentLayerIndex;

    private void SetAnimation(QuadSkeleton skeleton)
    {
        Dictionary<string, AnimationSlot> spineAnimationSlots = [];
        List<DrawOrder>                   drawOrders          = [];
        Deform                            deform              = new();
        float                             time                = 0f;
        foreach (var animation in skeleton.CombineAnimation.Data)
        {
            var drawOrder  = new DrawOrder { Time = time };
            drawOrders.Add(drawOrder);
            AddAttachments(animation, spineAnimationSlots, drawOrder, deform, time);
            RemoveAttachments(animation, spineAnimationSlots, time);
            time               = (animation.Key + 1) * Fps;
            _currentLayerIndex = 0;
        }

        _spineJsonData.Animations[skeleton.Name] = new SpineAnimation
        {
            Slots     = spineAnimationSlots,
            Deform    = deform,
            DrawOrder = drawOrders
        };
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
        });
#endif
#if DEBUG
        foreach (var drawOrder in drawOrders) drawOrder.SortOffset();
        drawOrders.RemoveAll(x => x.Offsets.Count == 0);
        spineAnimation.DrawOrder = drawOrders.Count != 0 ? drawOrders : null;
#endif
    }

    private void AddVertices(PoolData poolData, KeyframeLayer keyframeLayer, Deform deform, Timeline timeline,
        float                         time)
    {
        AnimationVertices vert = new() { Time = time };
        var animationDefaultValue =
            GetAnimationDefaultValue(deform, poolData.LayersData[0].SlotAndImageName, poolData.LayersData[0].SkinName);
        InterpolateAnimation(keyframeLayer, timeline, animationDefaultValue, vert);
    }

    private void InterpolateAnimation(KeyframeLayer layer, Timeline timeline, AnimationDefault animationDefault,
        AnimationVertices                           animationVert)
    {
        LineInterpolateAnimation(layer, animationDefault, animationVert, timeline);
    }

    private void LineInterpolateAnimation(KeyframeLayer layer,         AnimationDefault animationDefault,
        AnimationVertices                               animationVert, Timeline         timeline)
    {
        var vert = AnimationMatrixUtility.QuadMultiply(timeline.AnimationMatrix, layer.DstMatrix);
        // Make sure the image to center
        animationVert.Vertices = ProcessUtility.MinusFloats(vert.ToFloats(), layer.ZeroCenterPoints);
        animationDefault.ImageVertices.Add(animationVert);
    }

    private void RemoveAttachments(KeyValuePair<int, Attachment> animation,
        Dictionary<string, AnimationSlot>                        animationSlots,
        float                                                    time)
    {
        foreach (var timeline in animation.Value.ConcealAttachments)
        {
            var framePoint = timeline.FramePoint;
            switch (timeline.Attach?.AttachType)
            {
                case AttachType.Keyframe:
                    var keyframe = timeline.Attach as Keyframe;
                    ReleaseKeyframe(keyframe, animationSlots, time, framePoint);
                    break;
                case AttachType.Slot:
                    var slot = timeline.Attach as Slot;
                    keyframe = slot.Attaches.Find(x => x.AttachType == AttachType.Keyframe) as Keyframe;
                    ReleaseKeyframe(keyframe, animationSlots, time, framePoint);
                    break;
                case AttachType.HitBox:
                    var hitbox = timeline.Attach as Hitbox;
                    ReleaseHitbox(hitbox, animationSlots, time);
                    break;
            }
        }
    }

    private void AddAttachments(KeyValuePair<int, Attachment> animation,
        Dictionary<string, AnimationSlot> animationSlots, DrawOrder drawOrder, Deform deform, float time)
    {
        foreach (var timeline in animation.Value.DisplayAttachments)
        {
            var framePoint = timeline.FramePoint;

            switch (timeline.Attach?.AttachType)
            {
                case AttachType.Keyframe:
                    var keyframe = timeline.Attach as Keyframe;
                    GetKeyframe(keyframe, animationSlots, deform, timeline, time, framePoint, drawOrder);
                    break;
                case AttachType.Slot:
                    var slot = timeline.Attach as Slot;
                    keyframe = slot.Attaches.Find(x => x.AttachType == AttachType.Keyframe) as Keyframe;
                    GetKeyframe(keyframe, animationSlots, deform, timeline, time, framePoint, drawOrder);
                    break;
                case AttachType.HitBox:
                    var hitbox = timeline.Attach as Hitbox;
                    GetHitbox(hitbox, animationSlots, deform, time);
                    break;
            }
        }
    }

    private void ReleaseHitbox(Hitbox attachHitbox, Dictionary<string, AnimationSlot> animationSlots, float time)
    {
        foreach (var hitboxLayer in attachHitbox.Layer)
        {
            var slot = animationSlots[hitboxLayer.Name];
            slot.Attachment.Add(new AnimationAttachment
            {
                Time = time,
                Name = null
            });
        }
    }

    private void ReleaseKeyframe(Keyframe? attachKeyframe,
        Dictionary<string, AnimationSlot>  animationSlots,
        float                              time,
        FramePoint                         framePoint)
    {
        if (attachKeyframe is null) return;
        foreach (var layer in attachKeyframe.Layers)
        {
            var poolData = _pool.FindPoolData(layer, framePoint);
            RemoveAnimationSlots(poolData, animationSlots, time);
            _pool.Release(layer, poolData);
        }
    }

    private void RemoveAnimationSlots(PoolData poolData, Dictionary<string, AnimationSlot> animationSlots, float time)
    {
        var slot = animationSlots[poolData.LayersData[0].SlotAndImageName];
        slot.Attachment.Add(new AnimationAttachment
        {
            Time = time,
            Name = null
        });
    }

    private void GetHitbox(Hitbox attachHitbox, Dictionary<string, AnimationSlot> animationSlots, Deform deform,
        float                     time)
    {
        foreach (var hitboxLayer in attachHitbox.Layer)
        {
            var vert  = hitboxLayer.Hitquad;
            var value = GetAnimationDefaultValue(deform, hitboxLayer.Name, "Hitbox");
            AddHitboxLayerVertices(time, value, vert);
            AddHitboxAttachments(animationSlots, hitboxLayer.Name, time);
        }
    }

    private void AddHitboxAttachments(Dictionary<string, AnimationSlot> animationSlots, string layerName, float time)
    {
        if (!animationSlots.TryGetValue(layerName, out var value))
        {
            value                     = new AnimationSlot();
            animationSlots[layerName] = value;
        }

        value.Attachment.Add(new AnimationAttachment
        {
            Time = time,
            Name = layerName
        });
    }

    private static void AddHitboxLayerVertices(float time, AnimationDefault value, float[] vert)
    {
        value.ImageVertices.Add(new AnimationVertices
        {
            Time     = time,
            Vertices = vert
        });
    }

    private void GetKeyframe(Keyframe?    attachKeyframe,
        Dictionary<string, AnimationSlot> animationSlots,
        Deform                            deform,
        Timeline                          timeline,
        float                             time,
        FramePoint                        framePoint,
        DrawOrder                         drawOrder
    )
    {
        if (attachKeyframe is null) return;
        foreach (var layer in attachKeyframe.Layers)
        {
            var poolData = _pool.Get(layer);
            poolData.FramePoint = framePoint;
            AddSlots(poolData);
            AddAnimationSlots(poolData, animationSlots, time);
            AddVertices(poolData, layer, deform, timeline, time);
            AddLayerOffsets(drawOrder, poolData.LayersData[0].SlotAndImageName, _currentLayerIndex);
            _currentLayerIndex++;
        }
    }

    private void AddLayerOffsets(
        DrawOrder drawOrder,
        string    layerName,
        int       index)
    {
        drawOrder.LayerOffsets.Add(new DrawOrder.LayerOffset
        {
            LayerName      = layerName,
            Slot = _spineJsonData.SlotsDict[layerName],
            LayerIndex     = index
        });
    }

    private void AddAnimationSlots(PoolData poolData, Dictionary<string, AnimationSlot> slots, float time)
    {
        var slotName = poolData.LayersData[0].SlotAndImageName;
        if (!slots.TryGetValue(slotName, out var value))
        {
            value           = new AnimationSlot();
            slots[slotName] = value;
        }

        value.Attachment.Add(new AnimationAttachment
        {
            Time = time,
            Name = slotName
        });
    }

    private void AddSlots(PoolData poolData)
    {
        for (var index = 0; index < poolData.LayersData.Count; index++)
        {
            var layerData = poolData.LayersData[index];
            var skinName  = $"tex_id_{layerData.TexId}/skin_{index}";

            layerData.SkinName                = skinName;
            layerData.KeyframeLayer.LayerName = layerData.SlotAndImageName;

            var isAdded = _spineJsonData.SlotsDict.TryAdd(layerData.SlotAndImageName, new SpineSlot
            {
                Name       = layerData.SlotAndImageName,
                Attachment = layerData.SlotAndImageName,
                OrderByImageSlot    = layerData.KeyframeLayer.ImageNameOrder
            });
            if (!isAdded) continue;

            _spineJsonData.Slots.Add(_spineJsonData.SlotsDict[layerData.SlotAndImageName]);
            var skin = _spineJsonData.Skins.Find(x => x.Name.Equals(skinName));

            if (skin is null)
            {
                skin = new Skin { Name = skinName, Attachments = [] };
                _spineJsonData.Skins.Add(skin);
            }

            if (index == 0)
                InitBaseMesh(layerData, skin);
            else
                InitLinkedMesh(layerData, skin);
        }
    }

    private AnimationDefault GetAnimationDefaultValue(Deform deform, string slotName, string skinName)
    {
        if (!deform.SkinName.ContainsKey(skinName))
            deform.SkinName[skinName] = [];
        if (!deform.SkinName[skinName].TryGetValue(slotName, out var value))
        {
            value                               = new AnimationDefault { Name = slotName };
            deform.SkinName[skinName][slotName] = value;
        }

        return value;
    }

    private void InitBaseMesh(LayerData layerData, Skin skin)
    {
        skin.Attachments.Add(new Attachments
        {
            Mesh = new Mesh
            {
                Name     = layerData.SlotAndImageName,
                Uvs      = layerData.KeyframeLayer.UVs,
                Vertices = layerData.KeyframeLayer.ZeroCenterPoints
            }
        });
    }

    private void InitLinkedMesh(LayerData layerData, Skin skin)
    {
        skin.Attachments.Add(new Attachments
        {
            Mesh = new LinkedMesh
            {
                Name   = layerData.SlotAndImageName,
                Type   = "linkedmesh",
                Skin   = $"tex_id_{layerData.KeyframeLayer.TexId}/skin_0",
                Parent = layerData.BaseSkinAttackmentName
            }
        });
    }

    private void InitHitboxSlot(QuadJsonData quadJsonData)
    {
        foreach (var hitbox in quadJsonData.Hitbox)
        {
            for (var i = 0; i < hitbox.Layer.Count; i++)
            {
                var hitboxLayerName = $"{hitbox.Name}_{i}";
                hitbox.Layer[i].Name = hitboxLayerName;
                _spineJsonData.Slots.Add(new SpineSlot
                {
                    Name       = hitboxLayerName,
                    Attachment = "boundingbox",
                    OrderByImageSlot    = int.MaxValue
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

        _spineJsonData.Skins.Add(_hitboxSkin);
    }
}