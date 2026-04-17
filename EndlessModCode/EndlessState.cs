using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace EndlessMod.EndlessModCode;

/// <summary>
/// Holds the shared, in-memory endless-run state.
///
/// Multiplayer note: all connected players must have this mod installed.
/// Because <see cref="IterationCount"/> is incremented inside the same
/// Harmony patch that fires on every client, its value stays naturally
/// synchronised with the game's existing act-change synchronisation.
/// </summary>
public static class EndlessState
{
    private const int MaxRecentBosses = 3;

    /// <summary>
    /// The active <see cref="RunState"/> for the current run, set when the
    /// run starts.  Caching this avoids calling
    /// <c>RunManager.DebugOnlyGetState()</c> from every patch.
    /// </summary>
    public static RunState? CurrentRun { get; private set; }

    /// <summary>Current loop number (1 = first pass through the acts).</summary>
    public static int IterationCount { get; private set; } = 1;

    /// <summary>
    /// Ring-buffer of the last <see cref="MaxRecentBosses"/> boss encounter IDs
    /// fought across all acts, used to prevent the same boss appearing as act 1's
    /// boss on the very next loop.
    /// </summary>
    private static readonly Queue<ModelId> _recentBossIds = new();

    /// <summary>Read-only view of the recent boss IDs.</summary>
    public static IReadOnlyCollection<ModelId> RecentBossIds => _recentBossIds;

    /// <summary>Increment the iteration counter by one.</summary>
    public static void IncrementIteration()
    {
        IterationCount++;
        MainFile.Logger.Info($"[EndlessMod] Entering iteration {IterationCount}.");
    }

    /// <summary>
    /// Records the boss encounter from every act in the supplied list so they
    /// can be excluded from the next loop's act 1 boss selection.
    /// Only the <see cref="MaxRecentBosses"/> most-recent entries are kept.
    /// </summary>
    public static void RecordBossesFromActs(IReadOnlyList<ActModel> acts)
    {
        foreach (var act in acts)
        {
            _recentBossIds.Enqueue(act.BossEncounter.Id);
            if (_recentBossIds.Count > MaxRecentBosses)
                _recentBossIds.Dequeue();
        }

        MainFile.Logger.Info(
            $"[EndlessMod] Recorded recent boss IDs for exclusion: " +
            $"{string.Join(", ", _recentBossIds)}");
    }

    /// <summary>Returns <c>true</c> if <paramref name="encounterId"/> is in the recent-boss list.</summary>
    public static bool IsRecentBoss(ModelId encounterId) => _recentBossIds.Contains(encounterId);

    /// <summary>Reset back to the first iteration (called when a new run starts).</summary>
    public static void Reset(RunState runState)
    {
        CurrentRun = runState ?? throw new ArgumentNullException(nameof(runState));
        IterationCount = 1;
        _recentBossIds.Clear();
    }
}
