using QTSAvalonia.Helper;

namespace QTSCore.Render;

/// <summary>
///     Encapsulates animation playback control: the play/stop loop with cancellation
///     and looping, plus single-frame stepping. Created and driven by
///     <see cref="QTSAvalonia.ViewModels.Pages.PlayerViewModel" />.
/// </summary>
/// <remarks>
///     The player does not own any UI state. It reports frame changes and playing-state
///     changes back to the view model through the <see cref="SetCurrentFrame" /> and
///     <see cref="SetIsPlaying" /> callbacks so the view model can keep its observable
///     properties (and therefore the bound controls) in sync.
/// </remarks>
public class AnimationPlayer
{
    private CancellationTokenSource? _cts;
    private bool _isPlaying;

    /// <summary>
    ///     Called with the frame index that should be displayed. The view model typically
    ///     assigns this to its <c>CurrentFrame</c> observable property, which triggers redraw.
    /// </summary>
    public Action<int>? SetCurrentFrame { get; set; }

    /// <summary>
    ///     Called when the playing state changes so the view model can mirror it onto its
    ///     <c>IsPlaying</c> observable property (drives Play/Stop button enabled state).
    /// </summary>
    public Action<bool>? SetIsPlaying { get; set; }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            _isPlaying = value;
            SetIsPlaying?.Invoke(value);
        }
    }

    /// <summary>
    ///     Starts playback from <paramref name="startFrame" />. If already playing, stops
    ///     instead (toggle behavior preserved from the original implementation).
    /// </summary>
    /// <param name="startFrame">Frame to resume from.</param>
    /// <param name="totalFrames">Total number of frames in the animation.</param>
    /// <param name="isLoop">Whether playback should loop back to frame 0 at the end.</param>
    /// <param name="frameDelaySeconds">Per-frame delay in seconds (1 / fps).</param>
    /// <param name="hasAnimation">
    ///     Whether a valid animation timeline exists; when false a warning is emitted and nothing
    ///     plays.
    /// </param>
    public async Task PlayAsync(int startFrame, int totalFrames, bool isLoop, float frameDelaySeconds,
        bool hasAnimation)
    {
        if (!hasAnimation)
        {
            LoggerHelper.Warning("No valid animation to play");
            ToastHelper.Warn("WARNING", "No animation available to play");
            return;
        }

        if (IsPlaying)
        {
            StopPlayback();
            return;
        }

        IsPlaying = true;
        LoggerHelper.Info($"Playing animation from frame {startFrame}");

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            for (var i = startFrame; i <= totalFrames && !token.IsCancellationRequested;)
            {
                SetCurrentFrame?.Invoke(i);
                await Task.Delay(TimeSpan.FromSeconds(frameDelaySeconds), token);
                i++;
                if (i >= totalFrames && isLoop) i = 0;
            }

            LoggerHelper.Info("Animation playback completed");
        }
        catch (OperationCanceledException)
        {
            LoggerHelper.Debug("Animation playback cancelled");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("Animation playback error", ex);
        }
        finally
        {
            IsPlaying = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    /// <summary>
    ///     Cancels any in-flight playback and reports the stopped state.
    /// </summary>
    public void StopPlayback()
    {
        _cts?.Cancel();
        IsPlaying = false;
        LoggerHelper.Debug("Playback stopped");
    }

    /// <summary>Computes the next frame index, wrapping to 0 at the end.</summary>
    public int GetNextFrame(int currentFrame, int totalFrames)
    {
        return currentFrame >= totalFrames - 1 ? 0 : currentFrame + 1;
    }

    /// <summary>Computes the previous frame index, wrapping to the last frame at the start.</summary>
    public int GetPreviousFrame(int currentFrame, int totalFrames)
    {
        return currentFrame <= 0 ? totalFrames - 1 : currentFrame - 1;
    }
}