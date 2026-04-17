# Endless Runs – Slay the Spire 2 Mod

A Slay the Spire 2 mod that enables truly endless runs.

## Features

- **Loop instead of dying** – when you would normally face the Architect at
  the end of the last act and auto-die, the run loops back to Act 1 instead.
- **Iteration indicator** – a HUD label ("Iteration: N") in the top-right
  corner shows how many times you have looped through all acts.
- **Scaled enemy HP** – on every loop, all enemy max HP is multiplied by the
  current iteration number (`base HP × iteration`).  First pass is unchanged,
  second pass is ×2, third pass is ×3, etc.
- **Scaled enemy attack damage** – enemy attack damage increases by 20% per
  loop (`base damage × (1 + 0.2 × (iteration − 1))`).  First pass is
  unchanged, second pass is ×1.2, third pass is ×1.4, etc.
- **Continuous act numbering** – act numbers carry over across loops instead
  of resetting.  The second loop shows Act 4, 5, 6; the third shows Act 7, 8,
  9; and so on.
- **Boss rewards on loop** – at the end of each loop the game offers a full
  boss-style reward screen (relic, card choice, and gold) before returning to
  Act 1.
- **No repeated act 1 boss** – the mod tracks the bosses fought across recent
  acts and ensures Act 1's boss on the next loop is not one you just fought.
- **End-campaign dialog** – after completing the first full loop a modal
  dialog appears at the end of every act, letting you choose to continue to
  the next act / loop or end the campaign normally.
- **Multiplayer compatible** – all connected players must have the mod
  installed.  The game's built-in act-change synchronisation keeps every
  client in step; the iteration counter is kept consistent automatically.
- **Configurable settings** – adjust the HP scale multiplier, attack damage
  scale multiplier, and iteration-count HUD visibility through the in-game
  mod-settings screen (powered by BaseLib).

## Requirements

- Slay the Spire 2 (Early Access)
- [BaseLib](https://github.com/Alchyr/STS2-BaseLib) mod loader

## Building

1. Open `EndlessMod.csproj` in your IDE or via `dotnet build`.
2. The build copies the `.dll` and `EndlessMod.json` manifest to your STS2
   `mods/EndlessMod/` folder automatically (requires Steam install detected by
   `Sts2PathDiscovery.props`).
3. Use Godot 4.5.1 (MegaDot) to export a `.pck` file if you need Godot assets.

## Implementation Notes

| Component | File | Description |
|-----------|------|-------------|
| Entry point | `EndlessModCode/MainFile.cs` | Wires up Harmony patches and game events |
| State | `EndlessModCode/EndlessState.cs` | Tracks iteration count and recent boss history |
| Loop patch | `EndlessModCode/Patches/EndlessLoopPatch.cs` | Redirects `EnterNextAct` / `WinRun` to loop back; offers boss rewards; shows end-campaign dialog |
| HP scaling | `EndlessModCode/Patches/MonsterHpScalingPatch.cs` | Scales enemy HP on room entry |
| Attack scaling | `EndlessModCode/Patches/MonsterAttackScalingPatch.cs` | Scales enemy attack damage by +20% per iteration |
| Act display | `EndlessModCode/Patches/ActDisplayPatch.cs` | Offsets act numbers so they continue across loops |
| MP sync | `EndlessModCode/Patches/MultiplayerSyncPatch.cs` | Syncs iteration counter on client peers |
| Settings | `EndlessModCode/EndlessModConfig.cs` | BaseLib `SimpleModConfig` exposing HP/attack multipliers and HUD toggle |
| HUD label | `EndlessModCode/UI/IterationLabel.cs` | Godot Label showing current iteration |
| End-campaign dialog | `EndlessModCode/UI/EndCampaignDialog.cs` | Modal overlay letting the player continue or end the run |
