using System.Collections.Concurrent;
using QuadToSpine2D.Core.Data.Spine;
using QuadToSpine2D.Core.Utility;

namespace QuadToSpine2D.Core.Process;

public class NewProcessSpine2DJson
{
    private readonly SpineJsonData _spineJsonData = new();
    private readonly ConcurrentDictionary<string, SpineAnimation> _spineAnimations = [];
    private readonly Skin _hitboxSkin = new() { Name = "Hitbox", Attachments = [] };
    private QuadJsonData _quadJsonData;
    private const float Fps = 1 / 60f;
    private LayerDataPool _layerDataPool = new();
    
    public NewProcessSpine2DJson(QuadJsonData quadJsonData)
    {
        _quadJsonData = quadJsonData;
        _spineJsonData.SpineSkeletons.ImagesPath = GlobalData.ImageSavePath;
        _spineJsonData.Bones.Add(new SpineBone { Name = "root" });
        InitHitboxSlot(quadJsonData);
        _spineJsonData.Skins.Add(_hitboxSkin);
    }

    public SpineJsonData Process()
    {
        foreach (var skeleton in _quadJsonData.Skeleton)
        {
            var time = 0f;
            foreach (var animation in skeleton.CombineAnimation.Data)
            {
                AddAttachments(animation, time);

                RemoveAttachments(animation);
                
                time += animation.Key * Fps;
            }
        }

        return _spineJsonData;
    }

    private void RemoveAttachments(KeyValuePair<float, Attachment> animation)
    {
        foreach (var concealAttachment in animation.Value.ConcealAttachments)
        {
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
    }

    private void AddAttachments(KeyValuePair<float, Attachment> animation, float time)
    {
        foreach (var displayAttachment in animation.Value.DisplayAttachments)
        {
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
    }

    private void ReleaseHitbox(Hitbox attachHitbox)
    {
    }

    private void ReleaseKeyframe(Keyframe? attachKeyframe)
    {
        if(attachKeyframe is null) return;
        foreach (var layer in attachKeyframe.Layer!)
        {
            _layerDataPool.Release(layer.PoolData);
        }
    }

    private void GetHitbox(Hitbox attachHitbox, float time)
    {
        // throw new NotImplementedException();
    }
    private void GetKeyframe(Keyframe? attachKeyframe, float time)
    {
        if(attachKeyframe is null) return;
        foreach (var layer in attachKeyframe.Layer)
        {
            var poolData = _layerDataPool.Get(layer!);
            layer.PoolData = poolData;
            AddSlots(poolData);
        }
    }
    
    private void AddSlots(PoolData poolData)
    {
        foreach (var data in poolData.LayersData)
        {
            for (var index = 0; index < data.Value.Count; index++)
            {
                var layerData = data.Value[index];
                if (_spineJsonData.Slots.Exists(x => x.Name.Equals(layerData.ImageName)))
                    continue;
                _spineJsonData.Slots.Add(new SpineSlot
                {
                    Name = layerData.ImageName,
                    Attachment = layerData.ImageName,
                    OrderId = layerData.KeyframeLayer.OrderId
                });
                _spineJsonData.SlotsDict[layerData.ImageName] = _spineJsonData.Slots[^1];
                var skinIndex = index * layerData.KeyframeLayer.TexId;
                var skinName = $"tex_id_{layerData.TexId}/skin_{skinIndex}";
                if(!_spineJsonData.Skins.Exists(x=>x.Name.Equals(skinName)))
                    _spineJsonData.Skins.Add(new Skin { Name = skinName,Attachments = []});
                if (TryInitBaseMesh(layerData, skinIndex))
                    continue;
                InitLinkedMesh(layerData, skinIndex, index);
            }
        }
    }
    private bool TryInitBaseMesh(LayerData layerData, int skinIndex)
    {
        if (skinIndex != 0) return false;
        _spineJsonData.Skins[skinIndex].Attachments.Add(new Attachments
        {
            Value = new Mesh
            {
                Name = layerData.ImageName,
                Uvs = layerData.KeyframeLayer.UVs,
                Vertices = layerData.KeyframeLayer.ZeroCenterPoints,
            }
        });
        return true;
    }
    private void InitLinkedMesh(LayerData layerData, int skinIndex,int meshIndex)
    {
        var skin = _spineJsonData.Skins.Find(x => x.Name == $"tex_id_{layerData.KeyframeLayer.TexId}/skin_0");
        skin?.Attachments.Add(new Attachments
        {
            Value = new LinkedMesh
            {
                Name = layerData.ImageName,
                Type = "linkedmesh",
                Skin = $"tex_id_{layerData.KeyframeLayer.TexId}/skin_0",
                Parent = _spineJsonData.Skins[skinIndex].Attachments[meshIndex].Value.Name,
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
                    Name = hitboxLayerName,
                    Bone = "root",
                    Attachment = "boundingbox",
                    OrderId = int.MaxValue,
                });
                _hitboxSkin.Attachments.Add(new Attachments
                {
                    Value = new Boundingbox
                    {
                        Name = hitboxLayerName,
                        Type = "boundingbox",
                    }
                });
            }
        }
    }
}