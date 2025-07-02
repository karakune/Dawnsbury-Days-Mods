using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Witch;

public static class ClassFeats
{
	public static IEnumerable<Feat> CreateFeats()
	{
		HashSet<FeatName> addWitchTraitTo = [
			Familiars.ClassFeats.FNEnhancedFamiliar,
			Familiars.ClassFeats.FNIncredibleFamiliar,
			FeatName.Counterspell,
			FeatName.ReachSpell,
			FeatName.WidenSpell,
			FeatName.SteadySpellcastingMain
		];

		foreach (var feat in AllFeats.All.FindAll(feat => addWitchTraitTo.Contains(feat.FeatName)))
			feat.Traits.Add(WitchLoader.TWitch);
		
		yield return new TrueFeat(ModManager.RegisterFeatName("Cackle"), 1,
			"Your patron’s power fills you with confidence, letting you sustain a magical working even as a quick burst of laughter leaves your lips.",
			"You learn the cackle hex.", [WitchLoader.TWitch])
			.WithOnSheet(sheet => sheet.AddFocusSpellAndFocusPoint(WitchSpells.THex, Ability.Intelligence, WitchSpells.Cackle)
		);

		yield return new TrueFeat(ModManager.RegisterFeatName("CantripExpansionWitch", "Cantrip Expansion"), 1,
			"A greater understanding of your magic broadens your range of simple spells.",
			"You can prepare two additional cantrips each day.",
			[WitchLoader.TWitch]).WithOnSheet(values =>
		{
			values.PreparedSpells.GetValueOrDefault(WitchLoader.TWitch)?.Slots
				.Add(new FreePreparedSpellSlot(0, "CantripExpansion1"));
			values.PreparedSpells.GetValueOrDefault(WitchLoader.TWitch)?.Slots
				.Add(new FreePreparedSpellSlot(0, "CantripExpansion2"));
		});
	}
}