using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Familiars;

public static class FamiliarMasterDedication
{
	public static FeatName FNFamiliarMaster = ModManager.RegisterFeatName("FamiliarMasterDedication", "Familiar Master Dedication");
	
	public static IEnumerable<Feat> CreateFeats()
	{
		yield return new TrueFeat(FNFamiliarMaster, 2,
				"You have forged a mystical bond with a creature. This might have involved complex rituals and invocations, such as meditating under the moon until something crept out of the forest. Or maybe you just did each other a good turn, such as rescuing the beast from a trap or a foe, and then being rescued in turn. Whatever the details, you are now comrades until the end.", 
				"You gain a familiar. If you already have a familiar, you gain the Enhanced Familiar feat.",
				[Trait.Dedication])
			.WithOnSheet(sheet => sheet.GrantFeat(!sheet.HasFeat(ClassFeats.FNFamiliar) ? ClassFeats.FNFamiliar : GeneralFeats.FNEnhancedFamiliar));
	}
}