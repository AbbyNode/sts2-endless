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
    /// <summary>Current loop number (1 = first pass through the acts).</summary>
    public static int IterationCount { get; private set; } = 1;

    /// <summary>Increment the iteration counter by one.</summary>
    public static void IncrementIteration()
    {
        IterationCount++;
        MainFile.Logger.Info($"[EndlessMod] Entering iteration {IterationCount}.");
    }

    /// <summary>Reset back to the first iteration (called when a new run starts).</summary>
    public static void Reset()
    {
        IterationCount = 1;
    }
}
