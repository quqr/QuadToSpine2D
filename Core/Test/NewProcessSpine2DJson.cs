using System.Collections.Concurrent;
using QuadToSpine2D.Core.Data.Spine;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core.Process;

public class NewProcessSpine2DJson
{
    private const    float Fps = 1 / 60f;
    private readonly Skin _hitboxSkin = new() { Name = "Hitbox", Attachments = [] };
    private readonly Pool _pool = new();
    private readonly QuadJsonData _quadJsonData;
    private readonly ConcurrentDictionary<string, SpineAnimation> _spineAnimations = [];
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
        var time = 0f;
        foreach (var animation in skeleton.CombineAnimation.Data)
        {
            AddAttachments(animation, time);

            RemoveAttachments(animation);

            time += animation.Key;
        }
    }

    private void RemoveAttachments(KeyValuePair<float, Attachment> animation)
    {
        foreach (var concealAttachment in animation.Value.ConcealAttachments)
            switch (concealAttachment.Attach?.AttachType)
            {
                case AttachType.Keyframe:
                    ReleaseKeyframe(concealAttachment.Attach.Keyframe!);
                    break;
                case AttachType.Slot:
                    ReleaseKeyframe(concealAttachment.Attach.Keyframe!);
                    break;
                case AttachType.HitBox:
                    ReleaseHitbox(concealAttachment.Attach.Hitbox!);
                    break;
            }
    }

    private void AddAttachments(KeyValuePair<float, Attachment> animation, float time)
    {
        foreach (var displayAttachment in animation.Value.DisplayAttachments)
            switch (displayAttachment.Attach?.AttachType)
            {
                case AttachType.Keyframe:
                    GetKeyframe(displayAttachment.Attach.Keyframe, time);
                    break;
                case AttachType.Slot:
                    GetKeyframe(displayAttachment.Attach.Keyframe, time);
                    break;
                case AttachType.HitBox:
                    GetHitbox(displayAttachment.Attach.Hitbox, time);
                    break;
            }
    }

    private void ReleaseHitbox(Hitbox attachHitbox)
    {
    }

    private void ReleaseKeyframe(Keyframe? attachKeyframe)
    {
        if (attachKeyframe is null) return;
        foreach (var layer in attachKeyframe.Layers) _pool.Release(layer);
    }

    private void GetHitbox(Hitbox attachHitbox, float time)
    {
        // throw new NotImplementedException();
    }


    private void GetKeyframe(Keyframe? attachKeyframe, float time)
    {
        // Remove
        if (attachKeyframe is null) return;
        foreach (var layer in attachKeyframe.Layers)
        {
            var poolData = _pool.Get(layer);
            AddSlots(poolData);
        }
    }

    private void AddSlots(PoolData poolData)
    {
        for (var index = 0; index < poolData.LayersData.Count; index++)
        {
            var layerData = poolData.LayersData[index];
            var skinName  = $"tex_id_{layerData.TexId}/skin_{index}";

            var isAdded = _spineJsonData.SlotsDict.TryAdd(layerData.ImageName, new SpineSlot
            {
                Name       = layerData.ImageName,
                Attachment = layerData.ImageName,
                OrderId    = layerData.KeyframeLayer.OrderId
            });
            if (!isAdded) continue;
            _spineJsonData.Slots.Add(_spineJsonData.SlotsDict[layerData.ImageName]);

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

    private void InitBaseMesh(LayerData layerData, Skin skin)
    {
        skin.Attachments.Add(new Attachments
        {
            Mesh = new Mesh
            {
                Name     = layerData.ImageName,
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
                Name   = layerData.ImageName,
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