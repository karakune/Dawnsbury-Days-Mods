using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Familiars;

public static class ClassFeats
{
	public static IEnumerable<Feat> CreateFeats()
	{
		yield return new TrueFeat(ModManager.RegisterFeatName("Familiar"), 1,
			"You make a pact with a creature that serves you and assists your spellcasting.", "You gain a familiar.",
			[Trait.Magus, Trait.Sorcerer, Trait.Wizard])
			.WithOnSheet(sheet => sheet.AddSelectionOption(new SingleFeatSelectionOption("Familiar", "Familiar", 1,
				feat => feat.HasTrait(FamiliarFeats.TFamiliar)))
		);
	}
}