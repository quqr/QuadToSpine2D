using QTSAvalonia.Helper;
using QTSAvalonia.ViewModels.Pages;
using QTSCore.Data;
using QTSCore.Data.Quad;
using QTSCore.Data.Spine;
using QTSCore.Utility;

namespace QTSCore.Process;

public class ProcessSpine2DJson
{
    private readonly List<DrawOrder> _drawOrders = [];
    private readonly List<PoolData> _existAttachments = [];

    private readonly Skin _hitboxSkin = new()
    {
        Name = "Hitbox", Attachments = []
    };

    private readonly Pool _pool = new();
    private readonly QuadJsonData _quadJsonData;

    private readonly Dictionary<string, AnimationSlot> _spineAnimationSlots = [];
    private readonly SpineJsonData _spineJsonData = new();
    private Deform _deform = new();
    private float _time;

    public ProcessSpine2DJson(QuadJsonData quadJsonData)
    {
        _quadJsonData = quadJsonData;
        _spineJsonData.SpineSkeletons.ImagesPath = Instances.ConverterSetting.ImageSavePath;
        _spineJsonData.Bones.Add(new SpineBone
        {
            Name = "root"
        });
    }

    public SpineJsonData Process()
    {
        InitHitboxSlot(_quadJsonData);
        foreach (var skeleton in _quadJsonData.Skeleton)
        {
            if (skeleton is null) continue;
            LoggerHelper.Info($"Processing animation : {skeleton.Name}");
            SetAnimation(skeleton);
        }

        SortSlotsAndDrawOrder();

        return _spineJsonData;
    }

    private void SortSlotsAndDrawOrder()
    {
        _spineJsonData.Slots = _spineJsonData.Slots
            .OrderBy(x => x.OrderByImageSlot)
            .ThenBy(x => x.Name)
            .ToList();
        for (var index = 0; index < _spineJsonData.Slots.Count; index++)
            _spineJsonData.Slots[index].SlotOrder = index;

        foreach (var animation in _spineJsonData.Animations)
        {
            if (animation.Value.DrawOrder is null) continue;
            SortDrawOrderAsync(animation.Value, animation.Value.DrawOrder);
        }
    }

    private void SetAnimation(QuadSkeleton skeleton)
    {
        _spineAnimationSlots.Clear();
        _drawOrders.Clear();
        _existAttachments.Clear();
        _deform = new Deform();
        _time = 0f;

        foreach (var animation in skeleton.CombineAnimation.Data)
        {
            RemoveAttachments(animation);
            AddAttachments(animation);

            var drawOrder = new DrawOrder
            {
                Time = _time
            };
            _drawOrders.Add(drawOrder);
            AddLayerOffsets(_existAttachments, drawOrder);

            _time = (animation.Key + 1) * ConverterSettingViewModel.Fps;
        }

        var animationName = skeleton.Name;
        if (skeleton.CombineAnimation.IsMix) animationName += "_MIX";
        if (skeleton.CombineAnimation.IsLoop) animationName += "_LOOP";
        if (skeleton.CombineAnimation.Data.Count == 0) animationName += "_EMPTY";
        _spineJsonData.Animations[animationName] = new SpineAnimation
        {
            Slots = new Dictionary<string, AnimationSlot>(_spineAnimationSlots), Deform = _deform.Clone(),
            DrawOrder = [.._drawOrders]
        };
    }

    private void SortDrawOrderAsync(SpineAnimation spineAnimation, List<DrawOrder> drawOrders)
    {
#if RELEASE
        Task.Run(() =>
        {
            foreach (var drawOrder in drawOrders) drawOrder.SortOffset();
            drawOrders.RemoveAll(x => x.Offsets.Count == 0);
            spineAnimation.DrawOrder = drawOrders.Count != 0 ? drawOrders : null;
        });
#endif
#if DEBUG
        foreach (var drawOrder in drawOrders) drawOrder.SortOffset();
        // drawOrders must be null if it's empty, or it will cause error in Spine2D
        drawOrders.RemoveAll(x => x.Offsets.Count == 0);
        spineAnimation.DrawOrder = drawOrders.Count != 0 ? drawOrders : null;
#endif
    }

    private void AddVertices(PoolData poolData, KeyframeLayer keyframeLayer, Timeline timeline)
    {
        AnimationVertices vert = new()
        {
            Time = _time
        };
        var animationDefaultValue =
            GetAnimationDefaultValue(poolData.LayersData[0].SlotAndImageName, poolData.LayersData[0].SkinName);
        InterpolateAnimation(keyframeLayer, timeline, animationDefaultValue, vert);
    }

    private void InterpolateAnimation(KeyframeLayer layer,
        Timeline timeline,
        AnimationDefault animationDefault,
        AnimationVertices animationVert)
    {
        LineInterpolateAnimation(layer, animationDefault, animationVert, timeline);
    }
    

    private void LineInterpolateAnimation(KeyframeLayer layer,
        AnimationDefault animationDefault,
        AnimationVertices animationVert,
        Timeline timeline)
    {
        var vert = timeline.AnimationMatrix * layer.DstMatrix;
        // Make sure the image to center
        animationVert.Vertices = ProcessUtility.MinusFloats(vert.ToFloatArray(), layer.ZeroCenterPoints);
        animationDefault.ImageVertices.Add(animationVert);
    }

    private void RemoveAttachments(KeyValuePair<int, Attachment> animation)
    {
        foreach (var timeline in animation.Value.ConcealAttachments)
        {
            var framePoint = timeline.FramePoint;
            switch (timeline.Attach?.AttachType)
            {
                case AttachType.Keyframe:
                    var keyframe = timeline.Attach as Keyframe;
                    ReleaseKeyframe(keyframe, framePoint);
                    break;
                case AttachType.Slot:
                    var slot = timeline.Attach as Slot;
                    keyframe = slot?.Attaches?.First(x => x.AttachType == AttachType.Keyframe) as Keyframe;
                    ReleaseKeyframe(keyframe, framePoint);
                    break;
                case AttachType.HitBox:
                    //if (timeline.Attach is Hitbox hitbox) ReleaseHitbox(hitbox);
                    //break;
                case AttachType.Animation:
                case AttachType.Skeleton:
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void AddAttachments(KeyValuePair<int, Attachment> animation)
    {
        foreach (var timeline in animation.Value.DisplayAttachments)
        {
            var framePoint = timeline.FramePoint;

            switch (timeline.Attach?.AttachType)
            {
                case AttachType.Keyframe:
                    var keyframe = timeline.Attach as Keyframe;
                    GetKeyframe(keyframe, timeline, framePoint);
                    break;
                case AttachType.Slot:
                    var slot = timeline.Attach as Slot;
                    if (slot?.Attaches is null) continue;
                    keyframe = slot.Attaches.First(x => x.AttachType == AttachType.Keyframe) as Keyframe;
                    GetKeyframe(keyframe, timeline, framePoint);
                    break;
                case AttachType.HitBox:
                    // if (timeline.Attach is not Hitbox hitbox) continue;
                    // TODO: bugs
                    // GetHitbox(hitbox);
                    break;
            }
        }
    }

    private void ReleaseHitbox(Hitbox attachHitbox)
    {
        foreach (var hitboxLayer in attachHitbox.Layer)
        {
            var slot = _spineAnimationSlots[hitboxLayer.Name];
            slot.Attachment.Add(new AnimationAttachment
            {
                Time = _time, Name = null
            });
        }
    }

    private void ReleaseKeyframe(Keyframe? attachKeyframe, FramePoint framePoint)
    {
        if (attachKeyframe?.Layers is null) return;
        foreach (var layer in attachKeyframe.Layers)
        {
            if (layer is null) continue;
            var poolData = _pool.FindPoolData(layer, framePoint);
            _existAttachments.Remove(poolData);
            ReleaseAnimationSlots(poolData);
            _pool.Release(layer, poolData);
        }
    }

    private void ReleaseAnimationSlots(PoolData poolData)
    {
        var slot = _spineAnimationSlots[poolData.LayersData[0].SlotAndImageName];
        slot.Attachment.Add(new AnimationAttachment
        {
            Time = _time, Name = null
        });
    }

    private void GetHitbox(Hitbox attachHitbox)
    {
        foreach (var hitboxLayer in attachHitbox.Layer)
        {
            var vert = hitboxLayer.Hitquad;
            var value = GetAnimationDefaultValue(hitboxLayer.Name, "Hitbox");
            AddHitboxLayerVertices(value, vert);
            AddHitboxAttachments(hitboxLayer.Name);
        }
    }

    private void AddHitboxAttachments(string layerName)
    {
        if (!_spineAnimationSlots.TryGetValue(layerName, out var value))
        {
            value = new AnimationSlot();
            _spineAnimationSlots[layerName] = value;
        }

        value.Attachment.Add(new AnimationAttachment
        {
            Time = _time, Name = layerName
        });
    }

    private void AddHitboxLayerVertices(AnimationDefault value, float[] vert)
    {
        value.ImageVertices.Add(new AnimationVertices
        {
            Time = _time, Vertices = vert
        });
    }

    private void GetKeyframe(Keyframe? attachKeyframe,
        Timeline timeline,
        FramePoint framePoint)
    {
        if (attachKeyframe?.Layers is null) return;
        foreach (var layer in attachKeyframe.Layers)
        {
            if (layer is null) continue;
            var poolData = _pool.Get(layer);
            _existAttachments.Add(poolData);
            poolData.FramePoint = framePoint;
            AddSlots(poolData);
            AddAnimationSlots(poolData);
            AddVertices(poolData, layer, timeline);
        }
    }

    private void AddLayerOffsets(List<PoolData> existAttachments, DrawOrder drawOrder)
    {
        for (var index = 0; index < existAttachments.Count; index++)
            drawOrder.LayerOffsets.Add(new DrawOrder.LayerOffset
            {
                LayerName = existAttachments[index].LayersData[0].SlotAndImageName,
                Slot = _spineJsonData.SlotsDict[existAttachments[index].LayersData[0].SlotAndImageName],
                LayerIndex = index
            });
    }

    private void AddAnimationSlots(PoolData poolData)
    {
        var slotName = poolData.LayersData[0].SlotAndImageName;
        if (!_spineAnimationSlots.TryGetValue(slotName, out var value))
        {
            value = new AnimationSlot();
            _spineAnimationSlots[slotName] = value;
        }

        value.Attachment.Add(new AnimationAttachment
        {
            Time = _time, Name = slotName
        });
    }

    private void AddSlots(PoolData poolData)
    {
        for (var index = 0; index < poolData.LayersData.Count; index++)
        {
            var layerData = poolData.LayersData[index];
            var skinName = $"tex_id_{layerData.TexId}/skin_{index}";

            layerData.SkinName = skinName;
            layerData.KeyframeLayer.LayerName = layerData.SlotAndImageName;

            var isAdded = _spineJsonData.SlotsDict.TryAdd(layerData.SlotAndImageName, new SpineSlot
            {
                Name = layerData.SlotAndImageName,
                Attachment = layerData.SlotAndImageName,
                OrderByImageSlot = layerData.KeyframeLayer.ImageNameOrder,
                Blend = poolData.LayersData[0].BlendId <= 0 ? "normal" : "additive"
            });
            if (!isAdded) continue;

            _spineJsonData.Slots.Add(_spineJsonData.SlotsDict[layerData.SlotAndImageName]);
            var skin = _spineJsonData.Skins.Find(x => x.Name.Equals(skinName));

            if (skin is null)
            {
                skin = new Skin
                {
                    Name = skinName, Attachments = []
                };
                _spineJsonData.Skins.Add(skin);
            }

            if (index == 0)
                InitBaseMesh(layerData, skin);
            else
                InitLinkedMesh(layerData, skin);
        }
    }

    private AnimationDefault GetAnimationDefaultValue(string slotName, string skinName)
    {
        if (!_deform.SkinName.ContainsKey(skinName))
            _deform.SkinName[skinName] = [];
        if (!_deform.SkinName[skinName].TryGetValue(slotName, out var value))
        {
            value = new AnimationDefault
            {
                Name = slotName
            };
            _deform.SkinName[skinName][slotName] = value;
        }

        return value;
    }

    private void InitBaseMesh(LayerData layerData, Skin skin)
    {
        skin.Attachments.Add(new Attachments
        {
            Mesh = new Mesh
            {
                Name = layerData.SlotAndImageName, Uvs = layerData.KeyframeLayer.UVs,
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
                Name = layerData.SlotAndImageName, Type = "linkedmesh", Skin = $"tex_id_{layerData.TexId}/skin_0",
                Parent = layerData.BaseSkinAttachmentName
            }
        });
    }

    private void InitHitboxSlot(QuadJsonData quadJsonData)
    {
        foreach (var hitbox in quadJsonData.Hitbox)
        {
            if (hitbox is null) continue;
            for (var i = 0; i < hitbox.Layer.Length; i++)
            {
                var hitboxLayerName = $"{hitbox.Name}_{i}";
                hitbox.Layer[i].Name = hitboxLayerName;
                _spineJsonData.Slots.Add(new SpineSlot
                {
                    Name = hitboxLayerName, Attachment = "boundingbox", OrderByImageSlot = int.MaxValue
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