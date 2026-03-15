using System.Collections.Generic;
using System.Text;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Feats.Features;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Mods.DeployableFamiliars;

namespace Dawnsbury.Mods.Classes.Witch;

public static class WitchLoader
{
	public static Trait TWitch = ModManager.RegisterTrait("Witch", new TraitProperties("Witch", true)
	{
		IsClassTrait = true
	});

	public static Trait TFirstHex = ModManager.RegisterTrait("First Hex", new TraitProperties("", relevant: false));

	public static FeatName FNStarlessShadow = ModManager.RegisterFeatName("StarlessShadow", "Starless Shadow");
	public static FeatName FNFaithsFlamekeeper = ModManager.RegisterFeatName("FaithsFlamekeeper", "Faith's Flamekeeper");
	public static FeatName FNSpinnerOfThreads = ModManager.RegisterFeatName("SpinnerOfThreads", "Spinner of Threads");
	public static FeatName FNSilenceInSnow = ModManager.RegisterFeatName("SilenceInSnow", "Silence in Snow");
	
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
				"Your patron first contacted you at the witching hour, as your body lay paralyzed by sleep while your mind had yet to escape the waking world. Your patron might be a creature of the Netherworld or a long-forgotten spirit of twilight — all you remember of them are haunting eyes of moonlight, offering you power from the darkness.", 
				"Lesson of Night's Terrors"),
			WitchPatronFeat.Create(FNFaithsFlamekeeper, Trait.Divine, Skill.Religion, WitchSpells.StokeTheHeart, SpellId.Command, FamiliarAbilities.FNRestoredSpirit, 
				"Your patron contacted you in a moment your willpower was close to sputtering out. Their reassuring presence was like breath and kindling bringing an ember back aflame, their magic giving you the strength to carry on and bring others to your cause. Your patron is likely a divine being like an angel or aeon acting covertly, though the possibility exists they might be a more sinister entity, using you to unknown ends.",
				"Lesson of Fervor's Grasp"),
			WitchPatronFeat.Create(FNSpinnerOfThreads, Trait.Occult, Skill.Occultism, WitchSpells.NudgeFate, SpellId.TrueStrike, FamiliarAbilities.FNBalancedLuck, 
				"You met your patron in a memory of an encounter yet to come or a premonition of something long since passed, as they untangled and re-spun the tapestry of time and fate. Was your patron a norn? A herald of a deity of fate and destiny? Could it even be a single individual appearing at three or more points in its timeline — multiple versions of the same being, parallel threads converging on a single moment?",
				"Lesson of Fate's Vicissitudes"),
			WitchPatronFeat.Create(FNSilenceInSnow, Trait.Primal, Skill.Nature, WitchSpells.ClingingIce, WitchSpells.GustOfWind, FamiliarAbilities.FNFreezingRime, 
				"Bitter cold heralded your patron's appearance, in the depths of the winter solstice or on a frozen peak at the end of the world. Your patron might be a winter hag, ice yai, or other spirit of the cold, but one thing is clear as ice — their power is not to be underestimated.",
				"Lesson of Winter's Chill"),
		];

		yield return new Feat(ModManager.RegisterFeatName("FirstHexPatronsPuppet", "Patron's Puppet"),
				null, "Gain the {i}{link:PatronsPuppet}patron's puppet{/}{/i} hex and a focus point.", [TFirstHex], null)
			.WithRulesBlockForSpell(WitchSpells.PatronsPuppet, TWitch, 1)
			.WithOnSheet(sheet =>
				sheet.AddFocusSpellAndFocusPoint(WitchSpells.THex, Ability.Intelligence, WitchSpells.PatronsPuppet));

		yield return new Feat(ModManager.RegisterFeatName("FirstHexPhaseFamiliar", "Phase Familiar"),
				null, "Gain the {i}{link:PhaseFamiliar}phase familiar{/i} hex and a focus point.", [TFirstHex], null)
			.WithRulesBlockForSpell(WitchSpells.PhaseFamiliar, TWitch, 1)
			.WithOnSheet(sheet =>
				sheet.AddFocusSpellAndFocusPoint(WitchSpells.THex, Ability.Intelligence, WitchSpells.PhaseFamiliar));
		
		Feat witchClass = new ClassSelectionFeat(
				ModManager.RegisterFeatName("FeatWitch", "Witch"),
				"You command powerful magic, not through study or devotion to any ideal, but as a vessel or agent for a mysterious, otherworldly patron that even you don't entirely understand. This entity might be a covert divinity, a powerful fey, a manifestation of natural energies, an ancient spirit, or any other mighty supernatural being — but its nature is likely as much a mystery to you as it is to anyone else. Through a special familiar, your patron grants you versatile spells and powerful hexes to use as you see fit, though you're never certain if these gifts will end up serving your patron's larger plan.",
				TWitch,
				new EnforcedAbilityBoost(Ability.Intelligence),
				6,
				[Trait.Perception, Trait.Fortitude, Trait.Reflex, Trait.Simple, Trait.Unarmed, Trait.UnarmoredDefense],
				[Trait.Will],
				3,
				$$"""
				  {b}1. Patron.{/b} Your magic is granted to you by a patron, which determines your spellcasting tradition; and grants you a skill for that tradition, a unique familiar ability, and your first witch's lesson (you can learn additional lessons with feats). Most witch lessons teach you a hex spell and allow you to prepare a spell you wouldn't normally be able to prepare. 
				  
				  {b}2. Familiar.{/b} Your patron has sent you a powerful {link:ClassFamiliar}combat familiar{/}. Your familiar gains two additional familiar abilities: one is determined by your patron, and the other can be chosen each day as normal. You gain additional familiar abilities at higher levels.
				  
				  {b}3. Witch Spellcasting.{/b} {{S.DescribePreparedSpellcasting(Trait.Arcane, 2, Ability.Intelligence).Replace("you prepare", "you commune with your familiar to prepare").Replace("arcane spells", "spells of your patron's tradition")}}
				  
				  {b}4. Hex Spells.{/b} You gain a type of focus spell called a hex spell. A spell with the hex trait can only be cast once per turn. You learn your choice of the {i}{link:PatronsPuppet}patron's puppet{/}{/i} or the {i}{link:PhaseFamiliar}phase familiar{/}{/i} hex spell.
				  
				  Your patron's first witch's lesson also teaches you a hex cantrip, which are hex spells that don't cost focus points to use.
				  """,
				subclasses)
			.WithEffectiveClassFeatures(features => features
				.AddPreparedSpellcasting("two spell slots")
				.AddFeature(5, WellKnownClassFeature.ExpertInFortitude)
				.AddFeature(6, ExtraFamiliarAbility(5))
				.AddFeature(7, WellKnownClassFeature.ExpertInSpellcasting)
				.AddFeature(9, WellKnownClassFeature.ExpertInReflex)
				.AddFeature(11, WellKnownClassFeature.ExpertInPerception)
				.AddFeature(11, "Expert in simple weapons and unarmed attacks")
				.AddFeature(12, ExtraFamiliarAbility(6))
				.AddFeature(13, WellKnownClassFeature.ExpertInUnarmoredDefense)
				.AddFeature(13, WellKnownClassFeature.WeaponSpecialization)
				.AddFeature(15, WellKnownClassFeature.MasterInSpellcasting)
				.AddFeature(17, new ClassFeature(
					"Will of the Pupil",
					"Your proficiency rank for Will saves increases to master; when you roll a success on a Will save, you get a critical success instead.")
				{
					KeepCapitalization = true,
					OnSheet = values => values.SetProficiency(Trait.Will, Proficiency.Master),
					OnCreature = (values, self) => CommonCharacterFeatures.AddEvasion(values, self, "Will of the Pupil", Defense.Will)
				})
				.AddFeature(18, ExtraFamiliarAbility(7))
				.AddFeature(19, WellKnownClassFeature.LegendaryInSpellcasting))
			.WithOnSheet(sheet =>
			{
				sheet.AddSelectionOption(new SingleFeatSelectionOption("FirstHex", "First Hex", -1, feat => feat.HasTrait(TFirstHex)));
				sheet.GrantFeat(FeatName.ClassFamiliar);
				if (DeployableFamiliarTag.FindTag(sheet) is { } familiar)
					familiar.FamiliarAbilities += 1;
			});
		witchClass.RulesText = witchClass.RulesText.Replace("Key ability", "Key attribute");
		witchClass.RulesText = witchClass.RulesText.Replace("skills of your choice", "skills of your choice, as well as 1 skill determined by your patron");
		yield return witchClass;
	}

	public static ClassFeature ExtraFamiliarAbility(int total)
	{
		return new ClassFeature("Additional familiar ability (" + total + " abilities)")
		{
			OnSheet = values =>
			{
				if (DeployableFamiliarTag.FindTag(values) is { } familiar)
					familiar.FamiliarAbilities += 1;
			}
		};
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
		var famFeat = AllFeats.GetFeatByFeatName(familiarAbility);
		return new WitchPatronFeat(
				patronName,
				flavorText,
				$$"""
				  {b}Spell List:{/b} {{spellTradition}}
				  {b}Patron Skill:{/b} {{skill}}
				  {b}{{lessonName}}:{/b} You gain the {{AllSpells.CreateSpellLink(hexCantrip, WitchSpells.THex)}} hex cantrip and {{AllSpells.CreateSpellLink(extraPreparableSpell, WitchLoader.TWitch)}} is added to your preparable spell list.
				  {b}{{famFeat.Name}}:{/b} {{famFeat.RulesText}}
				  """)
			.WithOnSheet(sheet =>
			{
				sheet.GrantFeat(familiarAbility);
				sheet.SpellTraditionsKnown.Add(spellTradition);
				sheet.SetProficiency(Trait.Spell, Proficiency.Trained);
				sheet.TrainInThisOrSubstitute(skill);
				sheet.PreparedSpells.Add(WitchLoader.TWitch, new PreparedSpellSlots(Ability.Intelligence, spellTradition));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(0, "Witch:Cantrip1"));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(0, "Witch:Cantrip2"));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(0, "Witch:Cantrip3"));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(0, "Witch:Cantrip4"));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(0, "Witch:Cantrip5"));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(1, "Witch:Spell1-1"));
				sheet.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(1, "Witch:Spell1-2"));
				for (int i = 2; i <= 18; ++i)
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
				sheet.AddAtLevel(19, values => values.PreparedSpells[WitchLoader.TWitch].Slots.Add(new FreePreparedSpellSlot(10, "Witch:Spell10-1")));

				var repertoire = sheet.SpellRepertoires.GetOrCreate(WitchSpells.THex,
					() => new SpellRepertoire(Ability.Intelligence, spellTradition));
				repertoire.SpellsKnown.Add(AllSpells.CreateModernSpell(hexCantrip, null, sheet.MaximumSpellLevel,
					false, new SpellInformation
					{
						ClassOfOrigin = WitchSpells.THex
					}));
				
				sheet.PreparedSpells[WitchLoader.TWitch].AdditionalPreparableSpells.Add(extraPreparableSpell);
			});
	}
}