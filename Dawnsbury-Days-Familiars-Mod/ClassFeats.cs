using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Familiars;

public static class ClassFeats
{
	public static FeatName FNFamiliar = ModManager.RegisterFeatName("Familiar"); 
	public static FeatName FNArcaneThesisImpFam = ModManager.RegisterFeatName("ArcaneThesisImprovedFamiliar", "Improved Familiar Attunement"); 
	
	public static IEnumerable<Feat> CreateFeats()
	{
		yield return new TrueFeat(FNFamiliar, 1,
			"You make a pact with a creature that serves you and assists your spellcasting.", "You gain a familiar.",
			[Trait.Magus, Trait.Sorcerer, Trait.Wizard])
			.WithOnSheet(sheet => sheet.AddSelectionOption(new SingleFeatSelectionOption("Familiar", "Familiar", 1,
				feat => feat.HasTrait(FamiliarFeats.TFamiliar)))
		);

		yield return new Feat(FNArcaneThesisImpFam,
				"Your thesis is 'Familiars: An extensive study of the benefits of pets'.",
				"You gain the Familiar wizard feat. Your familiar gains an extra ability, and it gains an additional extra ability when you reach 6th, 12th, and 18th levels.",
				[Trait.ArcaneThesis], null)
			.WithOnSheet(sheet => sheet.GrantFeat(FNFamiliar));

	}
}