using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
// Note: the STS2 reference assembly uses lowercase 'sts2' in this namespace.
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace EndlessMod.EndlessModCode.UI;

/// <summary>
/// Manages the on-screen loop indicator displayed during a run.
///
/// The label is injected as a child of <c>NTopBarFloorIcon</c> so that it
/// appears directly next to the floor number in the format
/// <c>{Floor_Number} (loop: {Iteration})</c>.  It falls back to being a
/// child of <c>NRun.Instance</c> when the floor icon is not yet available.
///
/// Displayed text:  "(loop: 1)"  on the first pass,
///                  "(loop: 2)"  on the second pass, etc.
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

        var floorIcon = GetFloorIcon();
        if (floorIcon == null)
        {
            MainFile.Logger.Warn(
                "[EndlessMod] NTopBarFloorIcon not available yet – will retry on next room.");
            return;
        }

        _label = new Label
        {
            Name = LabelNodeName,
            Text = FormatLabel(),
        };

        // Match the visual style used by the top bar (white with dark outline).
        _label.AddThemeFontSizeOverride("font_size", 20);
        _label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        _label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f));
        _label.AddThemeConstantOverride("outline_size", 4);

        // AddChild must happen on the main thread; CallDeferred is safe here.
        // The label is added as a child of the floor icon so it appears
        // directly next to the floor number.
        floorIcon.CallDeferred(Node.MethodName.AddChild, _label);

        MainFile.Logger.Info("[EndlessMod] Loop label attached to NTopBarFloorIcon.");
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

        // If the label doesn't exist yet (e.g. setting was toggled on mid-run,
        // or CreateAndAttach was called before the floor icon was ready),
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
        $"(loop: {EndlessState.IterationCount})";

    private static NTopBarFloorIcon? GetFloorIcon()
    {
        var run = NRun.Instance;
        if (run == null)
        {
            MainFile.Logger.Warn("[EndlessMod] NRun.Instance is null – cannot locate floor icon.");
            return null;
        }

        var globalUi = run.GlobalUi;
        if (globalUi == null)
        {
            MainFile.Logger.Warn("[EndlessMod] NRun.GlobalUi is null – cannot locate floor icon.");
            return null;
        }

        var topBar = globalUi.TopBar;
        if (topBar == null)
        {
            MainFile.Logger.Warn("[EndlessMod] NGlobalUi.TopBar is null – cannot locate floor icon.");
            return null;
        }

        return topBar.FloorIcon;
    }

    private static void TryFindExisting()
    {
        var floorIcon = GetFloorIcon();
        if (floorIcon != null)
        {
            _label = floorIcon.FindChild(LabelNodeName, true, false) as Label;
            if (_label != null) return;
        }

        // Fallback: search the entire run scene tree.
        var runRoot = NRun.Instance;
        if (runRoot == null) return;
        _label = runRoot.FindChild(LabelNodeName, true, false) as Label;
    }
}
