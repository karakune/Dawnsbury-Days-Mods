using System.Collections.Generic;
using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Witch;

public static class ClassFeats
{
	public static Trait TSympStrike = ModManager.RegisterTrait("SympatheticStrike", new TraitProperties("", relevant: false));
	public static Trait TBasicLesson = ModManager.RegisterTrait("BasicLesson", new TraitProperties("", relevant: false));
	public static Trait TGreaterLesson = ModManager.RegisterTrait("GreaterLesson", new TraitProperties("", relevant: false));
	public static QEffect QSympStrikeUsed = new()
	{
		ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn
	}; 
	
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

		// var cauldron = ModManager.RegisterFeatName("Cauldron");
		// yield return new TrueFeat(cauldron, 2,
		// 	"",
		// 	"",
		// 	[WitchLoader.TWitch]).WithOnSheet(values =>
		// {
		// });

		var nails = new Item(IllustrationName.DragonClaws, "Eldritch Claws", Trait.Brawling, Trait.Agile,
				Trait.Unarmed, TSympStrike)
			.WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Slashing))
			.WithSoundEffect(SfxName.ScratchFlesh);
		var nailsFeat = new TrueFeat(ModManager.RegisterFeatName("WitchArmamentsNails", "Witch's Armaments (Eldritch Nails)"), 1,
				"Your patron’s power changes your body to ensure you are never defenseless.",
				"Your nails are supernaturally long and sharp. You gain a nails unarmed attack that deals 1d6 slashing damage, is in the brawling group, and has the agile and unarmed traits.",
				[WitchLoader.TWitch])
			.WithOnCreature(creature => creature.WithAdditionalUnarmedStrike(nails));
		yield return nailsFeat;

		var teeth = new Item(IllustrationName.Jaws, "Iron Teeth", Trait.Brawling, TSympStrike)
			.WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Piercing)).WithSoundEffect(SfxName.BiteApple);
		var teethFeat = new TrueFeat(ModManager.RegisterFeatName("WitchArmamentsTeeth", "Witch's Armaments (Iron Teeth)"), 1,
				"Your patron’s power changes your body to ensure you are never defenseless.",
				"With a click of your jaw, your teeth transform into long metallic points. You gain a jaws unarmed attack that deals 1d8 piercing damage and is in the brawling group.",
				[WitchLoader.TWitch])
			.WithOnCreature(creature =>
			{
				creature.WithAdditionalUnarmedStrike(teeth);
			});
		yield return teethFeat;

		var hair = new Item(IllustrationName.BlackTentacles, "Living Hair", Trait.Brawling, Trait.Agile,
				Trait.Disarm, Trait.Finesse, Trait.Trip, Trait.Unarmed, TSympStrike)
			.WithWeaponProperties(new WeaponProperties("1d4", DamageKind.Bludgeoning))
			.WithSoundEffect(SfxName.BiteApple); // TODO: sound effect
		var hairFeat =  new TrueFeat(ModManager.RegisterFeatName("WitchArmamentsHair", "Witch's Armaments (Living Hair)"), 1,
				"Your patron’s power changes your body to ensure you are never defenseless.",
				"You can instantly grow or shrink your hair, eyebrows, beard, or mustache by up to several feet and manipulate your hair for use as a weapon, though your control isn’t fine enough for more dexterous tasks. You gain a hair unarmed attack that deals 1d4 bludgeoning damage; is in the brawling group; and has the agile, disarm, finesse, trip, and unarmed traits.",
				[WitchLoader.TWitch])
			.WithOnCreature(creature => creature.WithAdditionalUnarmedStrike(hair));
		yield return hairFeat;

		yield return new TrueFeat(ModManager.RegisterFeatName("Sympathetic Strike"), 4,
				"You collect your patron’s magic into one of your witch armaments, causing them to shine with runes, light, or another signifier of your patron.",
				"Once per round, you can make an unarmed Strike with one with your witch’s armaments. If you hit, you establish a sympathetic link with the target, making it easier for your patron to affect them. Until the beginning of your next turn, the target takes a –1 circumstance penalty to its saves against your hexes, or a –2 penalty if the triggering Strike was a critical hit.",
				[WitchLoader.TWitch])
			.WithPrerequisite(new Prerequisite(sheet => sheet.HasFeat(nailsFeat) || sheet.HasFeat(teethFeat) || sheet.HasFeat(hairFeat), "You must have the feat Witch's Armaments."))
			.WithOnCreature(witch =>
			{
				var calculated = witch.PersistentCharacterSheet?.Calculated;
				if (calculated == null)
					return;
				
				witch.AddQEffect(new QEffect
				{
					ProvideStrikeModifier = item =>
					{
						if (witch.HasEffect(QSympStrikeUsed))
							return null;
							
						if (!item.HasTrait(TSympStrike))
							return null;
							
						var strikeModifiers = new StrikeModifiers
						{
							OnEachTarget = async (caster, target, result) =>
							{
								caster.AddQEffect(QSympStrikeUsed);
								switch (result)
								{
									case < CheckResult.Success:
										return;
									case CheckResult.Success:
										target.AddQEffect(new QEffect("Sympathetic Link", "You take a –1 circumstance penalty to saves against hexes", ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster)
										{
											BonusToDefenses = (effect, action, defense) => action != null && action.HasTrait(WitchSpells.THex) ? new Bonus(-1, BonusType.Circumstance, "Sympathetic Link") : null
										});
										break;
									case CheckResult.CriticalSuccess:
										target.AddQEffect(new QEffect("Sympathetic Link", "You take a –2 circumstance penalty to saves against hexes", ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster)
										{
											BonusToDefenses = (effect, action, defense) => action != null && action.HasTrait(WitchSpells.THex) ? new Bonus(-2, BonusType.Circumstance, "Sympathetic Link") : null
										});
										break;
								}
							}
						};
							
						var strike = witch.CreateStrike(item, strikeModifiers: strikeModifiers);
						strike.Name = item.Name + " (Sympathetic Strike)";
						return strike;
					}
				});
			});

		yield return new TrueFeat(ModManager.RegisterFeatName("Basic Lesson"), 2,
			"",
			"",
			[WitchLoader.TWitch],
			[
				CreateLesson(TBasicLesson, "Dreams", "Dreams can be a window to greater insights.", WitchSpells.VeilOfDreams, SpellId.Sleep),
				CreateLesson(TBasicLesson, "Life", "Life can be shared.", WitchSpells.LifeBoost, WitchSpells.SpiritLink),
				CreateLesson(TBasicLesson, "Protection", "An ounce of protection is worth a pound of cure.", WitchSpells.BloodWard, SpellId.MageArmor),
				CreateLessonElements(TBasicLesson, "Elements", "Natural disasters and inclement weather hold more power than the mightiest creature.", WitchSpells.ElementalBetrayal, [SpellId.BurningHands, WitchSpells.GustOfWind, SpellId.HydraulicPush, SpellId.PummelingRubble]),
				CreateLesson(TBasicLesson, "Vengeance", "Suffer not even the smallest slights.", WitchSpells.NeedleOfVengeance, SpellId.PhantomPain)
			]);

		yield return new TrueFeat(ModManager.RegisterFeatName("Greater Lesson"), 4,
				"",
				"",
				[WitchLoader.TWitch],
				[
					CreateLesson(TGreaterLesson, "Mischief", "Nothing's wrong with some mischief, now and then.", WitchSpells.DeceiverCloak, WitchSpells.MadMonkeys),
					CreateLesson(TGreaterLesson, "Shadow", "A shadow is far from empty — it contains something of the person who casts it.", WitchSpells.MaliciousShadow, SpellId.ChillingDarkness),
					CreateLesson(TGreaterLesson, "Snow", "Emulate snow, for it can snuff out life despite its gentleness.", WitchSpells.PersonalBlizzard, WitchSpells.WallOfWind)
				]);

		// yield return new TrueFeat(ModManager.RegisterFeatName("WitchBottle", "Witch's Bottle"), 8,
		// 	"",
		// 	"",
		// 	[WitchLoader.TWitch])
		// 	.WithPrerequisite(cauldron, "Cauldron");
	}

	private static Feat CreateLesson(Trait rank, string name, string flavorText, SpellId hex, SpellId spell)
	{
		return new Feat(ModManager.RegisterFeatName($"Lesson{name}", $"Lesson of {name}"), flavorText,
			$"You gain the {AllSpells.TemplateSpells[hex].Name} hex, and you add {AllSpells.TemplateSpells[spell].Name} to your spell list.",
			[rank], null)
			.WithOnSheet(sheet =>
			{
				sheet.AddFocusSpellAndFocusPoint(WitchLoader.TWitch, Ability.Intelligence, hex);
				sheet.SpellRepertoires[WitchLoader.TWitch].AdditionalSpellsAllowed.Add(spell);
			});
	}

	private static Feat CreateLessonElements(Trait rank, string name, string flavorText, SpellId hex, SpellId[] spell)
	{
		return new Feat(ModManager.RegisterFeatName($"Lesson{name}", $"Lesson of {name}"), flavorText,
				$"You gain the {AllSpells.TemplateSpells[hex].Name} hex, and you add {AllSpells.TemplateSpells[spell[0]].Name}, {AllSpells.TemplateSpells[spell[1]].Name}, {AllSpells.TemplateSpells[spell[2]].Name} and {AllSpells.TemplateSpells[spell[3]].Name} to your spell list.",
				[rank], null)
			.WithOnSheet(sheet =>
			{
				sheet.AddFocusSpellAndFocusPoint(WitchLoader.TWitch, Ability.Intelligence, hex);
				sheet.SpellRepertoires[WitchLoader.TWitch].AdditionalSpellsAllowed.AddRange(spell);
			});
	}
}