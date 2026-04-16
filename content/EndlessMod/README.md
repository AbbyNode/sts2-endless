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
- **Multiplayer compatible** – all connected players must have the mod
  installed.  The game's built-in act-change synchronisation keeps every
  client in step; the iteration counter is kept consistent automatically.

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
| State | `EndlessModCode/EndlessState.cs` | Tracks current iteration count |
| Loop patch | `EndlessModCode/Patches/EndlessLoopPatch.cs` | Redirects `EnterNextAct` / `WinRun` to loop back |
| HP scaling | `EndlessModCode/Patches/MonsterHpScalingPatch.cs` | Scales enemy HP on room entry |
| MP sync | `EndlessModCode/Patches/MultiplayerSyncPatch.cs` | Syncs iteration counter on client peers |
| HUD label | `EndlessModCode/UI/IterationLabel.cs` | Godot Label showing current iteration |
