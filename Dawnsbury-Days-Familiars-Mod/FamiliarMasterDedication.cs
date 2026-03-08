using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;

namespace Dawnsbury.Mods.DeployableFamiliars;

public static class FamiliarMasterDedication
{
	public static IEnumerable<Feat> CreateFeats()
	{
		// TODO: If Familiar Master is already registered, modify it for extra goodies.
		if (!ModManager.TryParse("Familiar Master", out Trait FamiliarMaster))
			yield break;
	}
}