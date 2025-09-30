using System.Collections.Generic;
using System.Text;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Witch;

public static class WitchLoader
{
	public static Trait TWitch = ModManager.RegisterTrait("Witch", new TraitProperties("Witch", true)
	{
		IsClassTrait = true
	});

	public static Trait TFirstHex = ModManager.RegisterTrait("First Hex", new TraitProperties("", relevant: false));

	private static FeatName FNStarlessShadow = ModManager.RegisterFeatName("StarlessShadow", "Starless Shadow");
	private static FeatName FNFaithsFlamekeeper = ModManager.RegisterFeatName("FaithsFlamekeeper", "Faith's Flamekeeper");
	private static FeatName FNSpinnerOfThreads = ModManager.RegisterFeatName("SpinnerOfThreads", "Spinner of Threads");
	private static FeatName FNSilenceInSnow = ModManager.RegisterFeatName("SilenceInSnow", "Silence in Snow");
	
	[DawnsburyDaysModMainMethod]
	public static void LoadMod()
	{
		foreach (var feat in FamiliarAbilities.CreateFeats())
			ModManager.AddFeat(feat);
		
		foreach (var feat in CreateFeats())
			ModManager.AddFeat(feat);
		
		foreach (var feat in ClassFeats.CreateFeats())
			ModManager.AddFeat(feat);
	}

	private static IEnumerable<Feat> CreateFeats()
	{
		List<Feat> subclasses = [
			WitchPatronFeat.Create(FNStarlessShadow, Trait.Occult, Skill.Occultism, WitchSpells.ShroudOfNight, SpellId.Fear, FamiliarAbilities.FNStalkingNight, 
				"Your patron first contacted you at the witching hour, as your body lay paralyzed by sleep while your mind had yet to escape the waking world. Your patron might be a creature of the Netherworld or a long-forgotten spirit of twilight—all you remember of them are haunting eyes of moonlight, offering you power from the darkness.", 
				"Lesson of Night's Terrors"),
			WitchPatronFeat.Create(FNFaithsFlamekeeper, Trait.Divine, Skill.Religion, WitchSpells.StokeTheHeart, SpellId.Command, FamiliarAbilities.FNRestoredSpirit, 
				"Your patron contacted you in a moment your willpower was close to sputtering out. Their reassuring presence was like breath and kindling bringing an ember back aflame, their magic giving you the strength to carry on and bring others to your cause. Your patron is likely a divine being like an angel or aeon acting covertly, though the possibility exists they might be a more sinister entity, using you to unknown ends.",
				"Lesson of Fervor's Grasp"),
			WitchPatronFeat.Create(FNSpinnerOfThreads, Trait.Occult, Skill.Occultism, WitchSpells.NudgeFate, SpellId.TrueStrike, FamiliarAbilities.FNBalancedLuck, 
				"You met your patron in a memory of an encounter yet to come or a premonition of something long since passed, as they untangled and re-spun the tapestry of time and fate. Was your patron a norn? A herald of a deity like Pharasma, Alseta, or Grandmother Spider? Could it even be a single individual appearing at three or more points in its timeline—multiple versions of the same being, parallel threads converging on a single moment?",
				"Lesson of Fate's Vicissitudes"),
			WitchPatronFeat.Create(FNSilenceInSnow, Trait.Primal, Skill.Nature, WitchSpells.ClingingIce, WitchSpells.GustOfWind, FamiliarAbilities.FNFreezingRime, 
				"Bitter cold heralded your patron's appearance, in the depths of the winter solstice or on a frozen peak at the end of the world. Your patron might be a winter hag, ice yai, or other spirit of the cold, but one thing is clear as ice—their power is not to be underestimated.",
				"Lesson of Winter's Chill"),
		];

		yield return new Feat(ModManager.RegisterFeatName("FirstHexPatronsPuppet", "Patron's Puppet"),
				null, "Gain the Patron's Puppet hex and a focus point.", [TFirstHex], null)
			.WithOnSheet(sheet =>
				sheet.AddFocusSpellAndFocusPoint(WitchSpells.THex, Ability.Intelligence, WitchSpells.PatronsPuppet));

		yield return new Feat(ModManager.RegisterFeatName("FirstHexPhaseFamiliar", "Phase Familiar"),
				null, "Gain the Phase Familiar hex and a focus point.", [TFirstHex], null)
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
				sheet.GrantFeat(Familiars.FamiliarFeats.FNWitchFamiliarBoost);
				sheet.GrantFeat(Familiars.ClassFeats.FNFamiliar);
			});
	}
}

public class WitchPatronFeat : Feat
{
	private WitchPatronFeat(FeatName patronName, string flavorText, string rulesText) 
		: base(patronName, flavorText, rulesText, new List<Trait>(), null)
	{
	}

	public static Feat Create(FeatName patronName, Trait spellTradition, Skill skill, SpellId hexCantrip, SpellId extraPreparableSpell, FeatName familiarAbility, string flavorText, string lessonName)
	{
		var featDescription = new StringBuilder();
		featDescription.Append($"\n{{b}}Spell List:{{/b}} {spellTradition}");
		featDescription.Append($"\n{{b}}Patron Skill:{{/b}} {skill}");
		featDescription.Append($"\n{{b}}{lessonName}:{{/b}} You gain the {AllSpells.CreateSpellLink(hexCantrip, WitchSpells.THex)} hex cantrip and {AllSpells.CreateSpellLink(extraPreparableSpell, WitchLoader.TWitch)} is added to your preparable spell list.");
		var famFeat = AllFeats.GetFeatByFeatName(familiarAbility);
		featDescription.Append($"\n{{b}}{famFeat.Name}:{{/b}} {famFeat.RulesText}");
		
		return new WitchPatronFeat(patronName, flavorText, featDescription.ToString())
			.WithOnSheet(sheet =>
			{
				sheet.GrantFeat(familiarAbility);
				sheet.SpellTraditionsKnown.Add(spellTradition);
				sheet.TrainInThisOrSubstitute(skill);
				sheet.PreparedSpells.Add(WitchLoader.TWitch, new PreparedSpellSlots(Ability.Intelligence, spellTradition));
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
						ClassOfOrigin = WitchSpells.THex
					}));
				
				sheet.PreparedSpells[WitchLoader.TWitch].AdditionalPreparableSpells.Add(extraPreparableSpell);
				
				sheet.AddAtLevel(5, values => values.SetProficiency(Trait.Fortitude, Proficiency.Expert));
				sheet.AddAtLevel(7, values => values.SetProficiency(Trait.Spell, Proficiency.Expert));
			});
	}
}