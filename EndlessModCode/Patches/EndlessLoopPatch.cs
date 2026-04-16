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
