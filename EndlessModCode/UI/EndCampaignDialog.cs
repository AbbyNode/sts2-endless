using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace EndlessMod.EndlessModCode.UI;

/// <summary>
/// Presents a modal overlay that lets the player choose between continuing
/// the run (next act or next loop) and ending the campaign.
///
/// <see cref="ShowAsync"/> returns <c>true</c> when the player picks
/// "Continue…" and <c>false</c> when the player picks "End Campaign".
/// </summary>
internal static class EndCampaignDialog
{
    // Canvas size assumed by the game.
    private const float CanvasWidth = 1920f;
    private const float CanvasHeight = 1080f;

    // Dialog dimensions.
    private const float PanelWidth = 520f;
    private const float PanelHeight = 220f;

    /// <summary>
    /// Shows the dialog and returns a task that completes when the player
    /// presses a button.  <c>true</c> = continue; <c>false</c> = end campaign.
    /// </summary>
    internal static Task<bool> ShowAsync(string continueButtonText)
    {
        var tcs = new TaskCompletionSource<bool>();

        var runRoot = NRun.Instance;
        if (runRoot == null)
        {
            MainFile.Logger.Warn(
                "[EndlessMod] NRun.Instance is null – end-campaign dialog skipped; defaulting to continue.");
            tcs.SetResult(true);
            return tcs.Task;
        }

        // ------------------------------------------------------------------
        // Semi-transparent full-screen overlay so the map is dimmed and no
        // underlying buttons can be clicked accidentally.
        // ------------------------------------------------------------------
        var overlay = new ColorRect
        {
            Name = "EndCampaignOverlay",
            Color = new Color(0f, 0f, 0f, 0.65f),
            Position = Vector2.Zero,
            Size = new Vector2(CanvasWidth, CanvasHeight),
        };

        // ------------------------------------------------------------------
        // Centred panel.
        // ------------------------------------------------------------------
        float panelX = (CanvasWidth - PanelWidth) / 2f;
        float panelY = (CanvasHeight - PanelHeight) / 2f;

        var panel = new Panel
        {
            Name = "EndCampaignPanel",
            Position = new Vector2(panelX, panelY),
            Size = new Vector2(PanelWidth, PanelHeight),
        };

        // ------------------------------------------------------------------
        // Layout: vertical box inside the panel.
        // ------------------------------------------------------------------
        var vbox = new VBoxContainer
        {
            Position = new Vector2(20f, 20f),
            Size = new Vector2(PanelWidth - 40f, PanelHeight - 40f),
        };

        var titleLabel = new Label
        {
            Text = "What would you like to do?",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 28);

        var continueBtn = new Button
        {
            Text = continueButtonText,
        };
        continueBtn.AddThemeFontSizeOverride("font_size", 22);

        var endBtn = new Button
        {
            Text = "End Campaign",
        };
        endBtn.AddThemeFontSizeOverride("font_size", 22);

        // Wire up buttons – QueueFree the overlay and resolve the TCS.
        void Resolve(bool choice)
        {
            if (overlay.IsInsideTree())
                overlay.QueueFree();
            tcs.TrySetResult(choice);
        }

        continueBtn.Pressed += () => Resolve(true);
        endBtn.Pressed += () => Resolve(false);

        vbox.AddChild(titleLabel);
        vbox.AddChild(continueBtn);
        vbox.AddChild(endBtn);
        panel.AddChild(vbox);
        overlay.AddChild(panel);

        // AddChild must run on the main thread; CallDeferred is safe here.
        runRoot.CallDeferred(Node.MethodName.AddChild, overlay);

        MainFile.Logger.Info("[EndlessMod] End-campaign dialog shown.");
        return tcs.Task;
    }
}
