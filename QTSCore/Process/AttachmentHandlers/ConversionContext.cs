using QTSCore.Data.Spine;
using QTSCore.Interfaces;

namespace QTSCore.Process.AttachmentHandlers;

/// <summary>
///     Holds the shared mutable state used by <see cref="IAttachmentHandler" /> implementations
///     while a single Spine2D animation is being converted.
/// </summary>
/// <remarks>
///     <para>
///         <c>ProcessSpine2DJson</c> owns a single instance of this context, resets its per-animation
///         state at the start of each animation, and passes it to every handler invocation. Keeping
///         the state here lets handlers stay stateless and avoids passing a long parameter list.
///     </para>
/// </remarks>
public class ConversionContext
{
    /// <summary>
    ///     The object pool used to allocate and recycle <see cref="PoolData" /> for keyframe layers.
    /// </summary>
    public required Pool Pool { get; init; }

    /// <summary>
    ///     The attachments that are currently displayed for the active animation.
    ///     Used to compute draw-order offsets.
    /// </summary>
    public required List<PoolData> ExistAttachments { get; init; }

    /// <summary>
    ///     The per-slot animation state (attachment timeline) being built for the active animation.
    /// </summary>
    public required Dictionary<string, AnimationSlot> SpineAnimationSlots { get; init; }

    /// <summary>
    ///     The Spine2D document being produced.
    /// </summary>
    public required SpineJsonData SpineJsonData { get; init; }

    /// <summary>
    ///     The vertex deform data being built for the active animation. Reassigned per animation.
    /// </summary>
    public Deform Deform { get; set; } = new();

    /// <summary>
    ///     The current timeline time (in seconds) within the active animation.
    /// </summary>
    public float Time { get; set; }

    /// <summary>
    ///     Returns the <see cref="AnimationDefault" /> for the given skin/slot pair, creating it lazily.
    /// </summary>
    /// <remarks>
    ///     Shared by keyframe and hitbox attachment processing, hence hosted on the context rather
    ///     than on a single handler.
    /// </remarks>
    public AnimationDefault GetAnimationDefaultValue(string slotName, string skinName)
    {
        if (!Deform.SkinName.ContainsKey(skinName))
            Deform.SkinName[skinName] = [];
        if (!Deform.SkinName[skinName].TryGetValue(slotName, out var value))
        {
            value = new AnimationDefault
            {
                Name = slotName
            };
            Deform.SkinName[skinName][slotName] = value;
        }

        return value;
    }
}