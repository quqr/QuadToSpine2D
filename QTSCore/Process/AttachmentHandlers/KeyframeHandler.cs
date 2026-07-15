using QTSCore.Data;
using QTSCore.Data.Quad;
using QTSCore.Data.Spine;
using QTSCore.Interfaces;
using QTSCore.Utility;

namespace QTSCore.Process.AttachmentHandlers;

/// <summary>
/// Handles <see cref="AttachType.Keyframe"/> attachments: allocates pool data, registers
/// slots/skins/meshes, records animation slots, and interpolates vertex deformations for
/// each keyframe layer.
/// </summary>
public class KeyframeHandler : IAttachmentHandler
{
    public AttachType AttachType => AttachType.Keyframe;

    public void Add(Timeline timeline, FramePoint framePoint, ConversionContext context)
    {
        var keyframe = timeline.Attach as Keyframe;
        GetKeyframe(keyframe, timeline, framePoint, context);
    }

    public void Remove(Timeline timeline, FramePoint framePoint, ConversionContext context)
    {
        var keyframe = timeline.Attach as Keyframe;
        ReleaseKeyframe(keyframe, framePoint, context);
    }

    /// <summary>
    /// Displays every layer of a keyframe attach. Exposed so <see cref="SlotHandler"/> can
    /// reuse the logic after unwrapping its inner keyframe.
    /// </summary>
    internal static void GetKeyframe(Keyframe? attachKeyframe,
        Timeline timeline,
        FramePoint framePoint,
        ConversionContext context)
    {
        if (attachKeyframe?.Layers is null) return;
        foreach (var layer in attachKeyframe.Layers)
        {
            if (layer is null) continue;
            var poolData = context.Pool.Get(layer);
            context.ExistAttachments.Add(poolData);
            poolData.FramePoint = framePoint;
            AddSlots(poolData, context);
            AddAnimationSlots(poolData, context);
            AddVertices(poolData, layer, timeline, context);
        }
    }

    /// <summary>
    /// Conceals every layer of a keyframe attach. Exposed so <see cref="SlotHandler"/> can
    /// reuse the logic after unwrapping its inner keyframe.
    /// </summary>
    internal static void ReleaseKeyframe(Keyframe? attachKeyframe, FramePoint framePoint, ConversionContext context)
    {
        if (attachKeyframe?.Layers is null) return;
        foreach (var layer in attachKeyframe.Layers)
        {
            if (layer is null) continue;
            var poolData = context.Pool.FindPoolData(layer, framePoint);
            context.ExistAttachments.Remove(poolData);
            ReleaseAnimationSlots(poolData, context);
            context.Pool.Release(layer, poolData);
        }
    }

    private static void AddVertices(PoolData poolData, KeyframeLayer keyframeLayer, Timeline timeline,
        ConversionContext context)
    {
        AnimationVertices vert = new()
        {
            Time = context.Time
        };
        var animationDefaultValue =
            context.GetAnimationDefaultValue(poolData.LayersData[0].SlotAndImageName, poolData.LayersData[0].SkinName);
        InterpolateAnimation(keyframeLayer, timeline, animationDefaultValue, vert);
    }

    private static void InterpolateAnimation(KeyframeLayer layer,
        Timeline timeline,
        AnimationDefault animationDefault,
        AnimationVertices animationVert)
    {
        LineInterpolateAnimation(layer, animationDefault, animationVert, timeline);
    }

    private static void LineInterpolateAnimation(KeyframeLayer layer,
        AnimationDefault animationDefault,
        AnimationVertices animationVert,
        Timeline timeline)
    {
        var vert = timeline.AnimationMatrix * layer.DstMatrix;
        // Make sure the image to center
        animationVert.Vertices = MathHelper.MinusFloats(vert.ToFloatArray(), layer.ZeroCenterPoints);
        animationDefault.ImageVertices.Add(animationVert);
    }

    private static void ReleaseAnimationSlots(PoolData poolData, ConversionContext context)
    {
        var slot = context.SpineAnimationSlots[poolData.LayersData[0].SlotAndImageName];
        slot.Attachment.Add(new AnimationAttachment
        {
            Time = context.Time, Name = null
        });
    }

    private static void AddAnimationSlots(PoolData poolData, ConversionContext context)
    {
        var slotName = poolData.LayersData[0].SlotAndImageName;
        if (!context.SpineAnimationSlots.TryGetValue(slotName, out var value))
        {
            value = new AnimationSlot();
            context.SpineAnimationSlots[slotName] = value;
        }

        value.Attachment.Add(new AnimationAttachment
        {
            Time = context.Time, Name = slotName
        });
    }

    private static void AddSlots(PoolData poolData, ConversionContext context)
    {
        var spineJsonData = context.SpineJsonData;
        for (var index = 0; index < poolData.LayersData.Count; index++)
        {
            var layerData = poolData.LayersData[index];
            var skinName = $"tex_id_{layerData.TexId}/skin_{index}";

            layerData.SkinName = skinName;
            layerData.KeyframeLayer.LayerName = layerData.SlotAndImageName;

            var isAdded = spineJsonData.SlotsDict.TryAdd(layerData.SlotAndImageName, new SpineSlot
            {
                Name = layerData.SlotAndImageName,
                Attachment = layerData.SlotAndImageName,
                OrderByImageSlot = layerData.KeyframeLayer.ImageNameOrder,
                Blend = poolData.LayersData[0].BlendId <= 0 ? "normal" : "additive"
            });
            if (!isAdded) continue;

            spineJsonData.Slots.Add(spineJsonData.SlotsDict[layerData.SlotAndImageName]);
            var skin = spineJsonData.Skins.Find(x => x.Name.Equals(skinName));

            if (skin is null)
            {
                skin = new Skin
                {
                    Name = skinName, Attachments = []
                };
                spineJsonData.Skins.Add(skin);
            }

            if (index == 0)
                InitBaseMesh(layerData, skin);
            else
                InitLinkedMesh(layerData, skin);
        }
    }

    private static void InitBaseMesh(LayerData layerData, Skin skin)
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

    private static void InitLinkedMesh(LayerData layerData, Skin skin)
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
}
