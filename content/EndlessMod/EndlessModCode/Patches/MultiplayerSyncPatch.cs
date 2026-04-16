using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using EndlessMod.EndlessModCode.UI;

namespace EndlessMod.EndlessModCode.Patches;

/// <summary>
/// Keeps the <see cref="EndlessState.IterationCount"/> consistent across all
/// players in a multiplayer session.
///
/// Strategy
/// --------
/// The game's existing act-change synchronisation already propagates the
/// <c>RunManager.EnterAct(0)</c> call to every peer, so all clients advance
/// to act 0 simultaneously.
///
/// The remaining problem is that client peers must increment their own
/// iteration counter at the same moment.  We accomplish this by subscribing
/// to <c>RunManager.ActEntered</c> (which fires on every peer whenever any
/// act starts) and detecting when we transition from the last act back to
/// act 0 – a pattern that never occurs during a normal forward run.
/// </summary>
internal static class MultiplayerSyncPatch
{
    // Track the act index from the *previous* ActEntered event so we can
    // detect a backward transition (last act → act 0 = a loop).
    private static int _previousActIndex = -1;

    /// <summary>
    /// Called once during mod initialisation to subscribe to the ActEntered
    /// event.
    /// </summary>
    internal static void Register()
    {
        RunManager.Instance.ActEntered += OnActEntered;
        RunManager.Instance.RunStarted += OnRunStarted;
    }

    private static void OnRunStarted(RunState _)
    {
        _previousActIndex = -1;
    }

    private static void OnActEntered()
    {
        var state = RunManager.Instance.DebugOnlyGetState();
        if (state == null)
        {
            _previousActIndex = -1;
            return;
        }

        int currentAct = state.CurrentActIndex;
        int lastActIndex = state.Acts.Count - 1;

        // A loop is detected when we move from the last act back to act 0.
        // On the host / singleplayer side, EndlessLoopPatches.DoEndlessLoop
        // already called EndlessState.IncrementIteration() before EnterAct(0)
        // resolved, so by the time ActEntered fires on the host the counter
        // is already correct.
        // On remote Client peers, DoEndlessLoop never ran directly, so we
        // must increment here.
        if (_previousActIndex == lastActIndex && currentAct == 0 && IsRemoteClient())
        {
            EndlessState.IncrementIteration();
            IterationLabel.Refresh();
        }

        _previousActIndex = currentAct;
    }

    // A peer is a "remote client" when the game service type is Client
    // (as opposed to Singleplayer or Host).
    private static bool IsRemoteClient() =>
        RunManager.Instance.NetService?.Type == NetGameType.Client;
}
