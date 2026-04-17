using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace EndlessMod.EndlessModCode.Patches;

/// <summary>
/// Scales all enemy attack-intent damage by an additional 20 % per iteration
/// beyond the first.
///
/// Formula: scaled damage = base damage × (1 + 0.2 × (iteration − 1))
///   Iteration 1 (first pass) → no change (×1.0).
///   Iteration 2             → ×1.2
///   Iteration 3             → ×1.4, etc.
///
/// Only attack intents are scaled (debuffs, status moves and other non-attack
/// intents are unaffected).  Scaling is also skipped for player-owned
/// creatures so that any player attack intents remain unchanged.
///
/// The patch fires on <see cref="AttackIntent.GetSingleDamage"/> because
/// that is the single source of truth for per-hit damage used by both the
/// HUD intent display and the combat damage pipeline.
/// </summary>
[HarmonyPatch(typeof(AttackIntent), nameof(AttackIntent.GetSingleDamage))]
internal static class MonsterAttackScalingPatch
{
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(ref int __result, Creature owner)
    {
        int iteration = EndlessState.IterationCount;
        if (iteration <= 1)
            return; // first pass – no scaling needed

        if (!owner.IsMonster)
            return; // only scale enemy attacks

        double scaleFactor = 1.0 + EndlessModConfig.AttackDamageScaleMultiplier * (iteration - 1);
        __result = (int)Math.Round(__result * scaleFactor);
    }
}
