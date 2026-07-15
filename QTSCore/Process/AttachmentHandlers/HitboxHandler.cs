using QTSCore.Data.Quad;
using QTSCore.Data.Spine;
using QTSCore.Interfaces;

namespace QTSCore.Process.AttachmentHandlers;

/// <summary>
/// Handles <see cref="AttachType.HitBox"/> attachments.
/// </summary>
/// <remarks>
/// Hitbox processing is currently disabled in the original implementation (the active code
/// path contained bugs), so <see cref="Add"/> and <see cref="Remove"/> are no-ops to preserve
/// existing behaviour. The supporting methods are kept here so the logic can be re-enabled in
/// a single place rather than re-introducing a switch branch in the orchestrator.
/// </remarks>
public class HitboxHandler : IAttachmentHandler
{
    public AttachType AttachType => AttachType.HitBox;

    public void Add(Timeline timeline, FramePoint framePoint, ConversionContext context)
    {
        // if (timeline.Attach is not Hitbox hitbox) return;
        // TODO: bugs
        // GetHitbox(hitbox, context);
    }

    public void Remove(Timeline timeline, FramePoint framePoint, ConversionContext context)
    {
        //if (timeline.Attach is Hitbox hitbox) ReleaseHitbox(hitbox, context);
        //break;
    }

    private static void ReleaseHitbox(Hitbox attachHitbox, ConversionContext context)
    {
        foreach (var hitboxLayer in attachHitbox.Layer)
        {
            var slot = context.SpineAnimationSlots[hitboxLayer.Name];
            slot.Attachment.Add(new AnimationAttachment
            {
                Time = context.Time, Name = null
            });
        }
    }

    private static void GetHitbox(Hitbox attachHitbox, ConversionContext context)
    {
        foreach (var hitboxLayer in attachHitbox.Layer)
        {
            var vert = hitboxLayer.Hitquad;
            var value = context.GetAnimationDefaultValue(hitboxLayer.Name, "Hitbox");
            AddHitboxLayerVertices(value, vert, context);
            AddHitboxAttachments(hitboxLayer.Name, context);
        }
    }

    private static void AddHitboxAttachments(string layerName, ConversionContext context)
    {
        if (!context.SpineAnimationSlots.TryGetValue(layerName, out var value))
        {
            value = new AnimationSlot();
            context.SpineAnimationSlots[layerName] = value;
        }

        value.Attachment.Add(new AnimationAttachment
        {
            Time = context.Time, Name = layerName
        });
    }

    private static void AddHitboxLayerVertices(AnimationDefault value, float[] vert, ConversionContext context)
    {
        value.ImageVertices.Add(new AnimationVertices
        {
            Time = context.Time, Vertices = vert
        });
    }
}
