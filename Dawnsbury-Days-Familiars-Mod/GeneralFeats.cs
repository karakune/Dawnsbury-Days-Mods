using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Familiars;

public static class GeneralFeats
{
	public static FeatName FNEnhancedFamiliar = ModManager.RegisterFeatName("EnhancedFamiliar", "Enhanced Familiar");
	public static FeatName FNIncredibleFamiliar = ModManager.RegisterFeatName("IncredibleFamiliar", "Incredible Familiar");

	public static IEnumerable<Feat> CreateFeats()
	{
		yield return new TrueFeat(FNEnhancedFamiliar, 2,
			"You infuse your familiar with additional primal energy, increasing its abilities.",
			"You can select four familiar or master abilities each day, instead of two.\n\n{b}Special{/b} [Witch] Add the bonus familiar abilities you gain for being a witch to this amount. [Wizard] If your arcane thesis is improved familiar attunement, your familiar’s base number of familiar abilities, before adding any extra abilities from the arcane thesis, is four.",
			[Trait.Druid, Trait.Magus, Trait.Sorcerer, Trait.Wizard]); // TODO: add Witch trait
		
		yield return new TrueFeat(FNIncredibleFamiliar, 8,
			"Your familiar is imbued with even more magic than other familiars.",
			"You can select a base of six familiar or master abilities each day, instead of four.\n\n\n{b}Special{/b} [Witch] Add the bonus familiar abilities you gain for being a witch to this amount.",
			[]) // TODO: add Witch trait
			.WithPrerequisite(FNEnhancedFamiliar, "Enhanced Familiar");
	}
	
}