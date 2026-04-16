using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace EndlessMod.EndlessModCode.Patches;

/// <summary>
/// Skips the Neow event when looping back to act 1 in endless mode.
///
/// On a normal first run the player enters act 1 and the Neow ancient event
/// offers its usual bonus choices.  When the endless loop sends the player
/// back to act 1 (iteration ≥ 2) those choices are not appropriate, so this
/// patch marks the event as pre-finished – the same mechanism the game uses
/// when restoring a completed event from a save – causing the act to start
/// directly at the map.
/// </summary>
[HarmonyPatch(typeof(EventModel), nameof(EventModel.BeginEvent))]
internal static class SkipNeowPatch
{
    [HarmonyPrefix]
    // ReSharper disable once InconsistentNaming
    private static void Prefix(EventModel __instance, ref bool isPreFinished)
    {
        if (__instance is Neow && EndlessState.IterationCount >= 2)
        {
            isPreFinished = true;
            MainFile.Logger.Info(
                $"[EndlessMod] Neow event marked as pre-finished for loop iteration {EndlessState.IterationCount}.");
        }
    }
}
