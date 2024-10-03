using System.Collections.Concurrent;
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
        // InitHitboxSlot(quadJsonData);
        // _spineJsonData.Skins.Add(_hitboxSkin);
    }

    public SpineJsonData Process()
    {
        var bar = 65f / _quadJsonData.Skeleton.Count;
        foreach (var skeleton in _quadJsonData.Skeleton)
        {
            GlobalData.BarTextContent = $"Processing animation : {skeleton.Name}";
            SetAnimation(skeleton);
            GlobalData.BarValue += bar;
        }

        return _spineJsonData;
    }

    private void SetAnimation(QuadSkeleton skeleton)
    {
        Dictionary<string, AnimationSlot> spineAnimationSlots = [];
        List<DrawOrder>?                   drawOrders          = [];
        Deform                            deform              = new();
        float                             time                = 0f;


        foreach (var animation in skeleton.CombineAnimation.Data)
        {
            AddAttachments(animation, spineAnimationSlots, deform, time);
            RemoveAttachments(animation, spineAnimationSlots, deform, time);
            time = animation.Key * Fps;
        }

        drawOrders = null;
        _spineJsonData.Animations[skeleton.Name] = new SpineAnimation
        {
            Slots     = spineAnimationSlots,
            Deform    = deform,
            DrawOrder = drawOrders
        };
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
        Dictionary<string, AnimationSlot>                          animationSlots,
        Deform                                                     deform,
        float                                                      time)
    {
        foreach (var timeline in animation.Value.ConcealAttachments)
        {
            var framePoint = timeline.FramePoint;
            switch (timeline.Attach?.AttachType)
            {
                case AttachType.Keyframe:
                    var keyframe = timeline.Attach as Keyframe;
                    ReleaseKeyframe(keyframe, animationSlots, time,framePoint);
                    break;
                case AttachType.Slot:
                    var slot = timeline.Attach as Slot;
                    keyframe = slot.Attaches.Find(x => x.AttachType == AttachType.Keyframe) as Keyframe;
                    ReleaseKeyframe(keyframe, animationSlots, time,framePoint);
                    break;
                case AttachType.HitBox:
                    var hitbox = timeline.Attach as Hitbox;
                    ReleaseHitbox(hitbox);
                    break;
            }
        }
    }

    private void AddAttachments(KeyValuePair<int, Attachment> animation,
        Dictionary<string, AnimationSlot>                       animationSlots, Deform deform, float time)
    {
        foreach (var timeline in animation.Value.DisplayAttachments)
        {
            var framePoint = timeline.FramePoint;
            switch (timeline.Attach?.AttachType)
            {
                case AttachType.Keyframe:
                    var keyframe = timeline.Attach as Keyframe;
                    GetKeyframe(keyframe, animationSlots, deform, timeline, time,framePoint);
                    break;
                case AttachType.Slot:
                    var slot = timeline.Attach as Slot;
                    keyframe = slot.Attaches.Find(x => x.AttachType == AttachType.Keyframe) as Keyframe;
                    GetKeyframe(keyframe, animationSlots, deform, timeline, time,framePoint);
                    break;
                case AttachType.HitBox:
                    var hitbox = timeline.Attach as Hitbox;
                    GetHitbox(hitbox);
                    break;
            }
        }
    }

    private void ReleaseHitbox(Hitbox attachHitbox)
    {
    }

    private void ReleaseKeyframe(Keyframe? attachKeyframe,
        Dictionary<string, AnimationSlot>  animationSlots,
        float                              time,
        FramePoint                              framePoint)
    {
        if (attachKeyframe is null) return;
        foreach (var layer in attachKeyframe.Layers)
        {
            var poolData = _pool.FindPoolData(layer, framePoint);
            RemoveAnimationSlots(poolData, animationSlots, time);
            _pool.Release(layer,poolData);
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

    private void GetHitbox(Hitbox attachHitbox)
    {
        // throw new NotImplementedException();
    }


    private void GetKeyframe(Keyframe?    attachKeyframe,
        Dictionary<string, AnimationSlot> animationSlots,
        Deform                            deform,
        Timeline                          timeline,
        float                             time,
        FramePoint                             framePoint)
    {
        if (attachKeyframe is null) return;
        foreach (var layer in attachKeyframe.Layers)
        {
            var poolData = _pool.Get(layer);
            poolData.FramePoint = framePoint;
            AddSlots(poolData);
            AddAnimationSlots(poolData, animationSlots, time);
            AddVertices(poolData, layer, deform, timeline, time);
        }
    }

    private void AddAnimationSlots(PoolData poolData, Dictionary<string, AnimationSlot> slots, float time)
    {
        var slotName = poolData.LayersData[0].SlotAndImageName;
        if (!slots.TryGetValue(slotName, out var value))
        {
            value           = new AnimationSlot();
            slots[slotName] = value;
        }
        
        var last = value.Attachment.LastOrDefault();
        if (ProcessUtility.ApproximatelyEqual(last?.Time, time) && last?.Name == slotName)
        {
            Console.WriteLine();
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
                OrderId    = layerData.KeyframeLayer.OrderId
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
}