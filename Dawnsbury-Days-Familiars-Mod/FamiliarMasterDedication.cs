using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;

namespace Dawnsbury.Mods.Familiars;

public static class FamiliarMasterDedication
{
	public static Trait TFamiliarMaster = ModManager.RegisterTrait("FamiliarMaster");
	public static FeatName FNEnhancedFamiliarDedication;
	public static FeatName FNIncredibleFamiliarDedication;
	
	public static IEnumerable<Feat> CreateFeats()
	{	
		yield return ArchetypeFeats.CreateAgnosticArchetypeDedication(TFamiliarMaster, 
			"You have forged a mystical bond with a creature. This might have involved complex rituals and invocations, such as meditating under the moon until something crept out of the forest. Or maybe you just did each other a good turn, such as rescuing the beast from a trap or a foe, and then being rescued in turn. Whatever the details, you are now comrades until the end.", 
			"You gain a familiar. If you already have a familiar, you gain the Enhanced Familiar feat.")
			.WithOnSheet(sheet => sheet.GrantFeat(!sheet.HasFeat(ClassFeats.FNFamiliar) ? ClassFeats.FNFamiliar : FNEnhancedFamiliarDedication));
		
		var enhancedFamiliar = ArchetypeFeats.DuplicateFeatAsArchetypeFeat(ClassFeats.FNEnhancedFamiliar, TFamiliarMaster, 4);
		FNEnhancedFamiliarDedication = enhancedFamiliar.FeatName;
		yield return enhancedFamiliar;
		
		var incredibleFamiliar = ArchetypeFeats.DuplicateFeatAsArchetypeFeat(ClassFeats.FNIncredibleFamiliar, TFamiliarMaster, 10);
		FNEnhancedFamiliarDedication = incredibleFamiliar.FeatName;
		yield return incredibleFamiliar;
	}
}