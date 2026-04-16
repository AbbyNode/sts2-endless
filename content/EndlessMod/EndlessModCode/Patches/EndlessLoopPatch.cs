using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using EndlessMod.EndlessModCode.UI;

namespace EndlessMod.EndlessModCode.Patches;

/// <summary>
/// Intercepts the two code-paths that end the run after the last act:
///
///   1. <c>RunManager.EnterNextAct()</c> – called to advance between acts.
///      When the player is already on the final act this would normally
///      trigger the Architect encounter / win sequence.  We redirect it to
///      loop back to act 0 instead.
///
///   2. <c>RunManager.WinRun()</c> – private async method called when all
///      acts have been completed.  This is a belt-and-suspenders fallback in
///      case <c>EnterNextAct</c> is not the entry-point for the win path.
///
/// A re-entrancy guard (<see cref="_isLooping"/>) ensures only one loop
/// transition runs even if both patches fire for the same event.
/// </summary>
internal static class EndlessLoopPatches
{
    // Prevents double-execution if both EnterNextAct and WinRun patches fire
    // for the same transition.
    private static bool _isLooping;

    // -----------------------------------------------------------------------
    //  Patch 1 – EnterNextAct
    // -----------------------------------------------------------------------

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
    private static class EnterNextActPatch
    {
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        private static bool Prefix(RunManager __instance, ref Task __result)
        {
            var state = __instance.DebugOnlyGetState();
            if (state == null)
                return true; // no run active; let original run

            // Only intercept when we are on the very last act.
            if (state.CurrentActIndex < state.Acts.Count - 1)
                return true; // normal inter-act transition; let original run

            // We are transitioning *out of* the last act – loop back instead.
            __result = DoEndlessLoop(__instance);
            return false; // skip original
        }
    }

    // -----------------------------------------------------------------------
    //  Patch 2 – WinRun (private, belt-and-suspenders)
    // -----------------------------------------------------------------------

    [HarmonyPatch(typeof(RunManager), "WinRun")]
    private static class WinRunPatch
    {
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        private static bool Prefix(RunManager __instance, ref Task __result)
        {
            // EnterNextAct patch already handled this; avoid double-looping.
            if (_isLooping)
            {
                __result = Task.CompletedTask;
                return false;
            }

            __result = DoEndlessLoop(__instance);
            return false; // skip original win sequence
        }
    }

    // -----------------------------------------------------------------------
    //  Shared loop logic
    // -----------------------------------------------------------------------

    private static async Task DoEndlessLoop(RunManager runManager)
    {
        if (_isLooping)
            return;

        _isLooping = true;
        try
        {
            EndlessState.IncrementIteration();

            // Regenerate fresh encounters for all acts so the second (and
            // subsequent) passes feel different from the first.
            runManager.GenerateRooms();

            // Return to act 0.  EnterAct handles the transition animation,
            // map generation, and act-change synchronisation in multiplayer.
            await runManager.EnterAct(0, true);

            // Refresh the HUD counter after the transition completes.
            IterationLabel.Refresh();

            MainFile.Logger.Info(
                $"[EndlessMod] Loop complete – now on iteration {EndlessState.IterationCount}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[EndlessMod] Error during endless loop: {ex}");
        }
        finally
        {
            _isLooping = false;
        }
    }
}
