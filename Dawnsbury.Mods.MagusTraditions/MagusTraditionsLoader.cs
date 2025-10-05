using System.Collections.Generic;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MagusTraditions;

public static class MagusTraditionsLoader
{
	public static Trait TTradition = ModManager.RegisterTrait("Magus Spell Tradition", new TraitProperties("Magus Spell Tradition", false));
	
	[DawnsburyDaysModMainMethod]
	public static void LoadMod()
	{	
		foreach (var feat in CreateFeats())
			ModManager.AddFeat(feat);
		
		AllFeats.GetFeatByFeatName(FeatName.Magus).WithOnSheet(sheet =>
		{
			sheet.AddSelectionOption(new SingleFeatSelectionOption("SpellTradition", "Magus Spell Tradition", -1, feat => feat.HasTrait(TTradition)));
		});
	}

	private static IEnumerable<Feat> CreateFeats()
	{
		yield return new Feat(ModManager.RegisterFeatName("SpellTraditionArcane", "Arcane Tradition"),
			null, "Choose the Arcane tradition as your spellcasting tradition.", [TTradition], null);
		
		yield return new Feat(ModManager.RegisterFeatName("SpellTraditionDivine", "Divine Tradition"),
				null, "Choose the Divine tradition as your spellcasting tradition.", [TTradition], null)
			.WithOnSheet(sheet =>
			{
				sheet.SpellTraditionsKnown.Remove(Trait.Arcane);
				sheet.SpellTraditionsKnown.Add(Trait.Divine);
				sheet.SpellTraditionsKnown.RemoveDuplicates();
				sheet.PreparedSpells[Trait.Magus].SpellTradition = Trait.Divine;
			});
		
		yield return new Feat(ModManager.RegisterFeatName("SpellTraditionOccult", "Occult Tradition"),
			null, "Choose the Occult tradition as your spellcasting tradition.", [TTradition], null)
			.WithOnSheet(sheet =>
			{
				sheet.SpellTraditionsKnown.Remove(Trait.Arcane);
				sheet.SpellTraditionsKnown.Add(Trait.Occult);
				sheet.SpellTraditionsKnown.RemoveDuplicates();
				sheet.PreparedSpells[Trait.Magus].SpellTradition = Trait.Occult;
			});

		yield return new Feat(ModManager.RegisterFeatName("SpellTraditionPrimal", "Primal Tradition"),
				null, "Choose the Primal tradition as your spellcasting tradition.", [TTradition], null)
			.WithOnSheet(sheet =>
			{
				sheet.SpellTraditionsKnown.Remove(Trait.Arcane);
				sheet.SpellTraditionsKnown.Add(Trait.Primal);
				sheet.SpellTraditionsKnown.RemoveDuplicates();
				sheet.PreparedSpells[Trait.Magus].SpellTradition = Trait.Primal;
			});
	}
}