using QTSCore.Data.Quad;
using QTSCore.Interfaces;

namespace QTSCore.Process.AttachmentHandlers;

/// <summary>
/// Handles <see cref="AttachType.Slot"/> attachments by unwrapping the contained keyframe
/// attach and delegating to <see cref="KeyframeHandler"/>.
/// </summary>
/// <remarks>
/// A slot attach is a container whose <see cref="Slot.Attaches"/> collection holds the real
/// attach(es) to display/conceal; only the inner <see cref="AttachType.Keyframe"/> is processed.
/// </remarks>
public class SlotHandler : IAttachmentHandler
{
    public AttachType AttachType => AttachType.Slot;

    public void Add(Timeline timeline, FramePoint framePoint, ConversionContext context)
    {
        var slot = timeline.Attach as Slot;
        if (slot?.Attaches is null) return;
        var keyframe = slot.Attaches.First(x => x.AttachType == AttachType.Keyframe) as Keyframe;
        KeyframeHandler.GetKeyframe(keyframe, timeline, framePoint, context);
    }

    public void Remove(Timeline timeline, FramePoint framePoint, ConversionContext context)
    {
        var slot = timeline.Attach as Slot;
        var keyframe = slot?.Attaches?.First(x => x.AttachType == AttachType.Keyframe) as Keyframe;
        KeyframeHandler.ReleaseKeyframe(keyframe, framePoint, context);
    }
}
