using BaseLib.Abstracts;
using BaseLib.Utils;
using CharMod.CharModCode.Character;
using CharMod.CharModCode.Extensions;

namespace CharMod.CharModCode.Relics;

[Pool(typeof(CharModRelicPool))]
public abstract class CharModRelic : CustomRelicModel
{
    public override string PackedIconPath => $"{Id.Entry.ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.ToLowerInvariant()}_outline.png".RelicImagePath();
    protected override string BigIconPath => $"{Id.Entry.ToLowerInvariant()}.png".BigRelicImagePath();
}