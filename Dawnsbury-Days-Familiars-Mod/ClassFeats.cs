using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Familiars;

public static class ClassFeats
{
	public static FeatName FNFamiliar = ModManager.RegisterFeatName("GetFamiliar", "Familiar"); 
	public static FeatName FNArcaneThesisImpFam = ModManager.RegisterFeatName("ArcaneThesisImprovedFamiliar", "Improved Familiar Attunement"); 
	public static FeatName FNEnhancedFamiliar = ModManager.RegisterFeatName("EnhancedFamiliar", "Enhanced Familiar");
	public static FeatName FNIncredibleFamiliar = ModManager.RegisterFeatName("IncredibleFamiliar", "Incredible Familiar");
	
	public static IEnumerable<Feat> CreateFeats()
	{
		yield return new TrueFeat(FNFamiliar, 1,
			"You make a pact with a creature that serves you and assists your spellcasting.", "You gain a familiar.",
			[Trait.Magus, Trait.Sorcerer, Trait.Wizard])
			.WithOnSheet(sheet => sheet.AddSelectionOption(new SingleFeatSelectionOption("Familiar", "Familiar", -1,
				feat => feat.HasTrait(FamiliarFeats.TFamiliar)))
			);

		yield return new Feat(FNArcaneThesisImpFam,
				"Your thesis is 'Familiars: An extensive study of the benefits of pets'.",
				"You gain the Familiar wizard feat. Your familiar gains an extra ability, and it gains an additional extra ability when you reach 6th, 12th, and 18th levels.",
				[Trait.ArcaneThesis], null)
			.WithOnSheet(sheet => sheet.GrantFeat(FNFamiliar));

		yield return new TrueFeat(FNEnhancedFamiliar, 2,
				"You infuse your familiar with additional primal energy, increasing its abilities.",
				"You can select four familiar or master abilities each day, instead of two.\n\n{b}Special{/b} [Witch] Add the bonus familiar abilities you gain for being a witch to this amount. [Wizard] If your arcane thesis is improved familiar attunement, your familiar’s base number of familiar abilities, before adding any extra abilities from the arcane thesis, is four.",
				[Trait.Druid, Trait.Magus, Trait.Sorcerer, Trait.Wizard])
			.WithPrerequisite(FNFamiliar, "Familiar")
			.WithOnSheet(sheet =>
				{
					var index = sheet.SelectionOptions.FindIndex(o => o.Key.EndsWith("FamiliarAbilities"));
					if (index > 0)
						sheet.SelectionOptions[index] = FamiliarFeats.CreateFamiliarFeatsSelectionOption(sheet);
				}
			);
		
		yield return new TrueFeat(FNIncredibleFamiliar, 8,
				"Your familiar is imbued with even more magic than other familiars.",
				"You can select a base of six familiar or master abilities each day, instead of four.\n\n\n{b}Special{/b} [Witch] Add the bonus familiar abilities you gain for being a witch to this amount.",
				[])
			.WithPrerequisite(FNEnhancedFamiliar, "Enhanced Familiar")
			.WithOnSheet(sheet =>
				{
					var index = sheet.SelectionOptions.FindIndex(o => o.Key.EndsWith("FamiliarAbilities"));
					if (index > 0)
						sheet.SelectionOptions[index] = FamiliarFeats.CreateFamiliarFeatsSelectionOption(sheet);
				}
			);
	}
}