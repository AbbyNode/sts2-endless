using BaseLib.Abstracts;
using Godot;

namespace CharMod.CharModCode.Character;

public class CharModRelicPool : CustomRelicPoolModel
{
    public override string EnergyColorName => CharMod.CharacterId;
    public override Color LabOutlineColor => CharMod.Color;
}