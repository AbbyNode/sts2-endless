using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
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
/// After the first loop (<see cref="EndlessState.IterationCount"/> ≥ 2) a
/// modal dialog is shown whenever any act ends, giving the player the choice
/// to continue or to end the campaign.
///
/// A re-entrancy guard (<see cref="_isLooping"/>) ensures only one loop
/// transition runs even if both patches fire for the same event.
/// </summary>
internal static class EndlessLoopPatches
{
    // Prevents double-execution if both EnterNextAct and WinRun patches fire
    // for the same transition.
    private static bool _isLooping;

    // Set to true for exactly one re-entrant call to EnterNextAct so the
    // original act-advance logic runs without triggering the dialog again.
    private static bool _skipDialogOnNextCall;

    // Set to true immediately before we invoke WinRun ourselves so the
    // WinRunPatch lets that call through to the real win sequence.
    private static bool _allowWinRun;

    // Cached reflection handle for the private RunManager.WinRun() method.
    private static readonly MethodInfo? WinRunMethod =
        typeof(RunManager).GetMethod("WinRun",
            BindingFlags.NonPublic | BindingFlags.Instance);

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

            // Re-entry: the dialog already resolved and we are forwarding to
            // the original EnterNextAct logic – skip our interception.
            if (_skipDialogOnNextCall)
            {
                _skipDialogOnNextCall = false;
                return true;
            }

            bool isLastAct = state.CurrentActIndex >= state.Acts.Count - 1;

            // After the first loop every act-end shows the choice dialog.
            if (EndlessState.IterationCount >= 2)
            {
                string continueText = isLastAct
                    ? "Continue to Next Loop"
                    : "Continue to Next Act";

                __result = ShowDialogAndAct(__instance, isLastAct, continueText);
                return false; // skip original; our async task takes over
            }

            // First pass: only intercept the last act to start the loop.
            if (isLastAct)
            {
                __result = DoEndlessLoop(__instance);
                return false;
            }

            return true; // normal inter-act transition; let original run
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
            // The player explicitly chose "End Campaign" – let the real win
            // sequence run.
            if (_allowWinRun)
            {
                _allowWinRun = false;
                return true;
            }

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
    //  Dialog + act-advance logic (runs after iteration 1)
    // -----------------------------------------------------------------------

    private static async Task ShowDialogAndAct(RunManager runManager, bool isLastAct, string continueText)
    {
        bool shouldContinue = await EndCampaignDialog.ShowAsync(continueText);

        if (shouldContinue)
        {
            if (isLastAct)
            {
                await DoEndlessLoop(runManager);
            }
            else
            {
                // Forward to the original EnterNextAct without triggering
                // our patch again.  Reset in finally so a thrown exception
                // never leaves the flag stuck.
                _skipDialogOnNextCall = true;
                try
                {
                    await runManager.EnterNextAct();
                }
                finally
                {
                    _skipDialogOnNextCall = false;
                }
            }
        }
        else
        {
            // Player chose "End Campaign": invoke WinRun, allowing our patch
            // to pass through to the real game win sequence.
            if (WinRunMethod != null)
            {
                _allowWinRun = true;
                try
                {
                    var result = WinRunMethod.Invoke(runManager, null) as Task;
                    if (result != null)
                        await result;
                }
                catch (Exception ex)
                {
                    // Reflection wraps the root cause in TargetInvocationException.
                    Exception root = ex.InnerException ?? ex;
                    MainFile.Logger.Error($"[EndlessMod] Error invoking WinRun: {root}");
                }
                finally
                {
                    _allowWinRun = false;
                }
            }
            else
            {
                MainFile.Logger.Error(
                    "[EndlessMod] WinRun method not found via reflection; cannot end campaign.");
            }
        }
    }

    // -----------------------------------------------------------------------
    //  Shared loop logic
    // -----------------------------------------------------------------------

    // Gold awarded to each player at the end of the last act.
    // Mirrors the approximate amount given by a mid-game act boss.
    private const int BossGoldMin = 95;
    private const int BossGoldMax = 105;

    // Number of card choices offered in the boss card-reward screen.
    private const int BossCardChoiceCount = 3;

    private static async Task DoEndlessLoop(RunManager runManager)
    {
        if (_isLooping)
            return;

        _isLooping = true;
        try
        {
            EndlessState.IncrementIteration();

            // Offer boss-style rewards (relic + card choice + gold) to every
            // player before the act transition so progression feels seamless.
            await OfferBossRewards(runManager);

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

    // -----------------------------------------------------------------------
    //  Boss reward helper
    // -----------------------------------------------------------------------

    /// <summary>
    /// Presents boss-style rewards (relic, card choice, and gold) to each
    /// player.  This mirrors the reward screen shown after the act 2 boss so
    /// the transition to the next loop feels rewarding rather than abrupt.
    ///
    /// Errors are logged and swallowed so that a failure here never prevents
    /// the run from looping back.
    /// </summary>
    private static async Task OfferBossRewards(RunManager runManager)
    {
        var state = runManager.DebugOnlyGetState();
        if (state == null)
            return;

        foreach (var player in state.Players)
        {
            try
            {
                var rewards = new List<Reward>
                {
                    new RelicReward(player),
                    new CardReward(
                        CardCreationOptions.ForRoom(player, RoomType.Boss),
                        BossCardChoiceCount,
                        player),
                    new GoldReward(BossGoldMin, BossGoldMax, player),
                };

                await RewardsCmd.OfferCustom(player, rewards);

                MainFile.Logger.Info(
                    $"[EndlessMod] Boss rewards offered to player {player}.");
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error(
                    $"[EndlessMod] Failed to offer boss rewards: {ex.Message}");
            }
        }
    }
}
