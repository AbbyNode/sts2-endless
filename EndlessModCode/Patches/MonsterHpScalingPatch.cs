using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace EndlessMod.EndlessModCode.Patches;

/// <summary>
/// Scales all enemy max-HP and current-HP by the current iteration count
/// each time a combat room is entered.
///
/// Formula: scaled HP = base HP × iteration
///   Iteration 1 (first pass) → no change.
///   Iteration 2 (second pass) → HP × 2.
///   Iteration 3 → HP × 3, etc.
///
/// Implementation uses reflection to access the private backing fields
/// <c>Creature._currentHp</c> and <c>Creature._maxHp</c>, consistent with
/// other STS2 mods (e.g. UndoAndRedo).
///
/// Multiplayer: all connected players run the same code on RoomEntered, so
/// enemy HP is scaled identically on every client.
///
/// This helper is called from <see cref="EndlessModCode.MainFile.OnRoomEntered"/>
/// rather than a Harmony patch so it runs after the event fires rather than
/// mid-async-method.
/// </summary>
internal static class MonsterHpScalingHelper
{
    // -----------------------------------------------------------------------
    //  Reflection caches (populated once at class-load time)
    // -----------------------------------------------------------------------

    /// <summary>CombatManager._state : CombatState</summary>
    private static readonly FieldInfo? CombatManagerStateField =
        AccessTools.Field(typeof(CombatManager), "_state");

    /// <summary>CombatState._enemies : list-like collection of enemy Creatures</summary>
    private static readonly FieldInfo? CombatStateEnemiesField =
        AccessTools.Field(typeof(CombatState), "_enemies");

    /// <summary>Creature._currentHp : int</summary>
    private static readonly FieldInfo? CreatureCurrentHpField =
        AccessTools.Field(typeof(Creature), "_currentHp");

    /// <summary>Creature._maxHp : int (backing field of the MaxHp property)</summary>
    private static readonly FieldInfo? CreatureMaxHpField =
        AccessTools.Field(typeof(Creature), "_maxHp");

    // -----------------------------------------------------------------------
    //  HP scaling logic
    // -----------------------------------------------------------------------

    internal static void ScaleEnemyHp()
    {
        int iteration = EndlessState.IterationCount;
        if (iteration <= 1)
            return; // first pass – no scaling needed

        if (CombatManagerStateField == null ||
            CombatStateEnemiesField == null ||
            CreatureCurrentHpField == null ||
            CreatureMaxHpField == null)
        {
            MainFile.Logger.Warn(
                "[EndlessMod] One or more reflection fields not found; enemy HP scaling skipped.");
            return;
        }

        var combatState = CombatManagerStateField.GetValue(CombatManager.Instance);
        if (combatState == null)
            return; // not in combat

        var enemies = CombatStateEnemiesField.GetValue(combatState) as IEnumerable<Creature>;
        if (enemies == null)
            return;

        int scaled = 0;
        foreach (var creature in enemies)
        {
            try
            {
                int baseMaxHp = (int)CreatureMaxHpField.GetValue(creature)!;
                int scaledMaxHp = (int)Math.Round(baseMaxHp * iteration * EndlessModConfig.HpScaleMultiplier);

                // Set both max and current HP so the creature starts at
                // full (scaled) health.
                CreatureMaxHpField.SetValue(creature, scaledMaxHp);
                CreatureCurrentHpField.SetValue(creature, scaledMaxHp);
                scaled++;
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn(
                    $"[EndlessMod] Failed to scale HP for a creature: {ex.Message}");
            }
        }

        if (scaled > 0)
        {
            MainFile.Logger.Info(
                $"[EndlessMod] Scaled {scaled} enemy creature(s) HP ×{iteration} (multiplier {EndlessModConfig.HpScaleMultiplier:F1}).");
        }
    }
}
