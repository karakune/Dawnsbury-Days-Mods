using System.Collections.Generic;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Dawnsbury.Mods.Familiars;

namespace Dawnsbury.Mods.Classes.Witch;

public class WitchLoader
{
	public static Trait TWitch = ModManager.RegisterTrait("Witch", new TraitProperties("Witch", true)
	{
		IsClassTrait = true
	});

	public static Trait TFirstHex = ModManager.RegisterTrait("First Hex");

	private static FeatName FNStarlessShadow = ModManager.RegisterFeatName("StarlessShadow", "Starless Shadow");
	
	[DawnsburyDaysModMainMethod]
	public static void LoadMod()
	{
		foreach (var feat in CreateFeats())
			ModManager.AddFeat(feat);

		foreach (var feat in FamiliarFeats.CreateFeats())
			ModManager.AddFeat(feat);

		foreach (var feat in FamiliarAbilities.CreateFeats())
			ModManager.AddFeat(feat);
	}

	private static IEnumerable<Feat> CreateFeats()
	{
		List<Feat> subclasses = [
			WitchPatronFeat.Create(FNStarlessShadow, Trait.Occult, Skill.Occultism, WitchSpells.ShroudOfNight, "")
		];

		yield return new Feat(ModManager.RegisterFeatName("FirstHexPatronsPuppet", "Patron's Puppet"),
				null, "Gain the Patron's Puppet focus spell and a focus point.", [TFirstHex], null)
			.WithOnSheet(sheet =>
				sheet.AddFocusSpellAndFocusPoint(WitchSpells.THex, Ability.Intelligence, WitchSpells.PatronsPuppet));

		yield return new Feat(ModManager.RegisterFeatName("FirstHexPhaseFamiliar", "Phase Familiar"),
				null, "Gain the Phase Familiar focus spell and a focus point.", [TFirstHex], null)
			.WithOnSheet(sheet =>
				sheet.AddFocusSpellAndFocusPoint(WitchSpells.THex, Ability.Intelligence, WitchSpells.PhaseFamiliar));
		
		yield return new ClassSelectionFeat(ModManager.RegisterFeatName("FeatWitch", "Witch"),
			"The Witch",
			TWitch,
			new EnforcedAbilityBoost(Ability.Intelligence),
			6,
			[Trait.Perception, Trait.Fortitude, Trait.Reflex, Trait.Simple, Trait.Unarmed, Trait.UnarmoredDefense, Trait.Spell],
			[Trait.Will],
			3,
			"", subclasses)
			.WithOnSheet(sheet =>
			{
				sheet.AddSelectionOption(new SingleFeatSelectionOption("FirstHex", "First Hex", -1, feat => feat.HasTrait(TFirstHex)));
				sheet.GrantFeat(FamiliarFeats.FNWitchFamiliarBoost);
				sheet.AddSelectionOption(new SingleFeatSelectionOption("Familiar", "Familiar", 1, feat => feat.HasTrait(FamiliarFeats.TFamiliar)));
			});
	}
}

public class WitchPatronFeat : Feat
{
	private WitchPatronFeat(FeatName patronName, string flavorText) 
		: base(patronName, flavorText, "XX", new List<Trait>(), null)
	{
	}

	public static Feat Create(FeatName patronName, Trait spellTradition, Skill skill, SpellId hexCantrip, string flavorText)
	{
		return new WitchPatronFeat(patronName, flavorText)
			.WithOnSheet(sheet =>
			{
				sheet.SpellTraditionsKnown.Add(spellTradition);
				sheet.TrainInThisOrSubstitute(skill);
				sheet.PreparedSpells.Add(WitchLoader.TWitch, new PreparedSpellSlots(Ability.Intelligence, Trait.Occult));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(0, "Witch:Cantrip1"));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(0, "Witch:Cantrip2"));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(0, "Witch:Cantrip3"));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(0, "Witch:Cantrip4"));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(0, "Witch:Cantrip5"));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(1, "Witch:Spell1-1"));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(1, "Witch:Spell1-2"));
				for (int i = 2; i <= 20; ++i)
				{
					int thisLevel = i;
					if (thisLevel % 2 == 1)
						sheet.AddAtLevel(thisLevel, values =>
						{
							int level = (thisLevel + 1) / 2;
							values.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(level, $"Witch:Spell{level}-1"));
							values.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(level, $"Witch:Spell{level}-2"));
						});
					else
						sheet.AddAtLevel(thisLevel, values =>
						{
							int level = thisLevel / 2;
							values.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(level, $"Witch:Spell{level}-3"));
						});
				}

				var repertoire = sheet.SpellRepertoires.GetOrCreate(WitchSpells.THex,
					() => new SpellRepertoire(Ability.Intelligence, spellTradition));
				repertoire.SpellsKnown.Add(AllSpells.CreateModernSpell(hexCantrip, null, sheet.MaximumSpellLevel,
					false, new SpellInformation
					{
						ClassOfOrigin = WitchLoader.TWitch
					}));
				
				sheet.AddAtLevel(5, values => values.SetProficiency(Trait.Fortitude, Proficiency.Expert));
				sheet.AddAtLevel(7, values => values.SetProficiency(Trait.Spell, Proficiency.Expert));
			});
	}
}