using QTSCore.Data.Quad;
using QTSCore.Process;
using QTSCore.Process.AttachmentHandlers;

namespace QTSCore.Interfaces;

/// <summary>
/// Strategy for processing a single <see cref="AttachType"/> during Spine2D conversion.
/// </summary>
/// <remarks>
/// <para>
/// Each implementation owns the add (display) and remove (conceal) logic for one
/// <see cref="AttachType"/>. Replacing the inline switch statements in
/// <c>ProcessSpine2DJson</c> with a registry of handlers means a new attachment type
/// only requires adding a new handler and registering it, without editing the
/// conversion orchestrator.
/// </para>
/// </remarks>
public interface IAttachmentHandler
{
    /// <summary>
    /// The attachment type this handler is responsible for.
    /// </summary>
    AttachType AttachType { get; }

    /// <summary>
    /// Called when the attachment should be displayed at the given timeline frame.
    /// </summary>
    /// <param name="timeline">The timeline carrying the attach to display.</param>
    /// <param name="framePoint">The frame point the attach is bound to.</param>
    /// <param name="context">The shared mutable conversion state.</param>
    void Add(Timeline timeline, FramePoint framePoint, ConversionContext context);

    /// <summary>
    /// Called when the attachment should be concealed at the given timeline frame.
    /// </summary>
    /// <param name="timeline">The timeline carrying the attach to conceal.</param>
    /// <param name="framePoint">The frame point the attach is bound to.</param>
    /// <param name="context">The shared mutable conversion state.</param>
    void Remove(Timeline timeline, FramePoint framePoint, ConversionContext context);
}
