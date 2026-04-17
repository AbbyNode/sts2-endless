using BaseLib.Config;

namespace EndlessMod.EndlessModCode;

/// <summary>
/// Mod settings exposed through the BaseLib mod-config UI.
///
/// Properties are persisted to disk automatically by BaseLib and displayed
/// in the in-game mod-settings screen.
/// </summary>
internal class EndlessModConfig : SimpleModConfig
{
    /// <summary>
    /// Multiplier applied to the per-iteration HP scale factor.
    /// The effective HP multiplier each loop is:
    ///   <c>iteration × HpScaleMultiplier</c>
    /// Default 1.0 preserves the original behaviour (×1, ×2, ×3 …).
    /// </summary>
    [ConfigSlider(0.0, 5.0, 0.1, Format = "×{0:F1}")]
    public static float HpScaleMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Per-iteration attack-damage increase factor.
    /// The effective damage multiplier each loop is:
    ///   <c>1 + AttackDamageScaleMultiplier × (iteration − 1)</c>
    /// Default 0.2 preserves the original behaviour (+20 % per loop).
    /// </summary>
    [ConfigSlider(0.0, 2.0, 0.05, Format = "+{0:P0}/loop")]
    public static float AttackDamageScaleMultiplier { get; set; } = 0.2f;

    /// <summary>
    /// Whether the "Iteration: N" HUD label is visible during a run.
    /// </summary>
    public static bool ShowIterationCount { get; set; } = true;
}
