using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace EndlessMod.EndlessModCode.UI;

/// <summary>
/// Manages the on-screen iteration indicator displayed during a run.
///
/// The label is injected as a child of <c>NRun.Instance</c> (the root node
/// of the in-run scene) each time a run starts, and removed when a new run
/// is created.
///
/// Displayed text:  "Iteration: 1"  on the first pass,
///                  "Iteration: 2"  on the second pass, etc.
/// </summary>
public static class IterationLabel
{
    private const string LabelNodeName = "EndlessIterationLabel";

    private static Label? _label;

    // -----------------------------------------------------------------------
    //  Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Called when a run starts: injects the label into the scene tree.
    /// Uses <c>CallDeferred</c> so it is safe to call before the run scene
    /// has finished initialising.
    /// </summary>
    internal static void CreateAndAttach()
    {
        // Remove any stale instance from a previous run.
        Detach();

        // Respect the user's "show iteration count" setting.
        if (!EndlessModConfig.ShowIterationCount)
            return;

        var runRoot = NRun.Instance;
        if (runRoot == null)
        {
            MainFile.Logger.Warn(
                "[EndlessMod] NRun.Instance is null – iteration label cannot be shown yet.");
            return;
        }

        _label = new Label
        {
            Name = LabelNodeName,
            Text = FormatLabel(),
            // Top-right area of the 1920×1080 canvas.
            Position = new Vector2(1600f, 20f),
        };

        // Readable font size.
        _label.AddThemeFontSizeOverride("font_size", 32);

        // White text with a dark outline for visibility over any background.
        _label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        _label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f));
        _label.AddThemeConstantOverride("outline_size", 4);

        // AddChild must happen on the main thread; CallDeferred is safe here.
        runRoot.CallDeferred(Node.MethodName.AddChild, _label);

        MainFile.Logger.Info("[EndlessMod] Iteration label attached to NRun.");
    }

    /// <summary>
    /// Update the label text to reflect <see cref="EndlessState.IterationCount"/>.
    /// Safe to call even when no label is currently attached.
    /// </summary>
    internal static void Refresh()
    {
        // If the setting was toggled off, remove any existing label.
        if (!EndlessModConfig.ShowIterationCount)
        {
            Detach();
            return;
        }

        // If the label doesn't exist yet (e.g. setting was toggled on mid-run),
        // try to create it now.
        if (_label == null || !GodotObject.IsInstanceValid(_label))
            TryFindExisting();

        if (_label == null || !GodotObject.IsInstanceValid(_label))
        {
            CreateAndAttach();
            return;
        }

        _label.Text = FormatLabel();
    }

    /// <summary>
    /// Remove the label from the scene tree.
    /// </summary>
    internal static void Detach()
    {
        if (_label != null && GodotObject.IsInstanceValid(_label))
            _label.QueueFree();

        _label = null;
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    private static string FormatLabel() =>
        $"Iteration: {EndlessState.IterationCount}";

    private static void TryFindExisting()
    {
        var runRoot = NRun.Instance;
        if (runRoot == null) return;

        _label = runRoot.FindChild(LabelNodeName, true, false) as Label;
    }
}
