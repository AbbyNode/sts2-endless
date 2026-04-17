using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;

namespace EndlessMod.EndlessModCode.Patches;

/// <summary>
/// Patches <c>NActBanner.Create</c> so that act numbers continue to increment
/// across loops rather than resetting to "Act 1" each iteration.
///
/// On the first pass the display is unchanged (Act 1, Act 2, Act 3).
/// On the second pass the same three acts are displayed as Act 4, Act 5, Act 6.
/// On the third pass they appear as Act 7, Act 8, Act 9, and so on.
///
/// The offset applied to <c>actIndex</c> is:
///   <c>(IterationCount - 1) × numberOfActs</c>
/// where <c>numberOfActs</c> is <c>RunState.Acts.Count</c> (typically 3).
/// </summary>
[HarmonyPatch(typeof(NActBanner), nameof(NActBanner.Create))]
internal static class ActDisplayPatch
{
    [HarmonyPrefix]
    // ReSharper disable once InconsistentNaming
    private static void Prefix(ActModel act, ref int actIndex)
    {
        int iteration = EndlessState.IterationCount;
        if (iteration <= 1)
            return; // first pass – no offset needed

        var state = EndlessState.CurrentRun;
        if (state == null)
            return;

        int totalActs = state.Acts.Count;
        actIndex += (iteration - 1) * totalActs;

        MainFile.Logger.Info(
            $"[EndlessMod] ActDisplayPatch: displaying act index {actIndex} (iteration {iteration}, {totalActs} acts/loop).");
    }
}
