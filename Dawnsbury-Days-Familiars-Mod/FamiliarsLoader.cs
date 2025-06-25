using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Familiars;

public static class FamiliarsLoader
{
	[DawnsburyDaysModMainMethod]
	public static void LoadMod()
	{
		foreach (var feat in FamiliarAbilities.CreateFeats())
			ModManager.AddFeat(feat);
	
		foreach (var feat in FamiliarFeats.CreateFeats())
			ModManager.AddFeat(feat);
		
		foreach (var feat in MasterAbilities.CreateFeats())
			ModManager.AddFeat(feat);
		
		foreach (var feat in ClassFeats.CreateFeats())
			ModManager.AddFeat(feat);
		
		foreach (var feat in FamiliarMasterDedication.CreateFeats())
			ModManager.AddFeat(feat);

		ModManager.AddFeat(new TrueFeat(ModManager.RegisterFeatName("GnomeFamiliar", "Animal Accomplice"), 1,
				"You build a rapport with an animal, which becomes magically bonded to you.", "You gain a Familiar.",
				[Trait.Gnome])
			.WithOnSheet(sheet => sheet.GrantFeat(ClassFeats.FNFamiliar))
		);
	}
}