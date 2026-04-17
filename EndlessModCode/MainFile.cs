using System.Reflection;
using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;
using EndlessMod.EndlessModCode.Patches;
using EndlessMod.EndlessModCode.UI;

namespace EndlessMod.EndlessModCode;

/// <summary>
/// Entry point for the Endless Runs mod.
/// Initialises Harmony patches and wires up run-lifecycle events.
/// </summary>
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "EndlessMod";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        // Reset the endless state whenever a fresh run begins.
        RunManager.Instance.RunStarted += OnRunStarted;

        // Scale enemy HP and refresh the HUD whenever a room is entered.
        RunManager.Instance.RoomEntered += OnRoomEntered;

        // Register the multiplayer sync hook that fires on every ActEntered.
        MultiplayerSyncPatch.Register();

        // Register mod settings with the BaseLib config system.
        ModConfigRegistry.Register(ModId, new EndlessModConfig());

        Logger.Info("Endless Runs mod initialised.");
    }

    private static void OnRunStarted(RunState runState)
    {
        EndlessState.Reset(runState);
        Logger.Info("New run started – endless state reset.");
        // Create the iteration HUD label for this run.
        IterationLabel.CreateAndAttach();
    }

    private static void OnRoomEntered()
    {
        // Scale enemy HP when a combat room is entered.
        MonsterHpScalingHelper.ScaleEnemyHp();
        // Keep the HUD label up-to-date.
        IterationLabel.Refresh();
    }
}
