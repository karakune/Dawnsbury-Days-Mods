using System.Collections.Generic;
using System.Threading.Tasks;
using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Witch;

public static class ClassFeats
{
	public static Trait TSympStrike = ModManager.RegisterTrait("SympatheticStrike");
	public static QEffect QSympStrikeUsed = new()
	{
		ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn,
		PreventTakingAction = ca => ca.HasTrait(TSympStrike) ? "You already used Sympathetic Strike this round." : null,
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
				Trait.Unarmed)
			.WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Slashing))
			.WithSoundEffect(SfxName.ScratchFlesh);
		var nailsSymp = new Item(IllustrationName.DragonClaws, "Eldritch Claws (Sympathetic Strike)", Trait.Brawling, Trait.Agile,
				Trait.Unarmed)
			.WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Slashing))
			.WithSoundEffect(SfxName.ScratchFlesh)
			.WithAdditionalWeaponProperties(property => property.WithOnTarget(ApplySympatheticLink));
		var nailsFeat = new TrueFeat(ModManager.RegisterFeatName("WitchArmamentsNails", "Witch's Armaments (Eldritch Nails)"), 1,
				"Your patron’s power changes your body to ensure you are never defenseless.",
				"Your nails are supernaturally long and sharp. You gain a nails unarmed attack that deals 1d6 slashing damage, is in the brawling group, and has the agile and unarmed traits.",
				[WitchLoader.TWitch, TSympStrike])
			.WithTag(nailsSymp)
			.WithOnCreature(creature => creature.WithAdditionalUnarmedStrike(nails));
		yield return nailsFeat;

		var teeth = new Item(IllustrationName.Jaws, "Iron Teeth", Trait.Brawling)
			.WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Piercing)).WithSoundEffect(SfxName.BiteApple);
		var teethSymp = new Item(IllustrationName.Jaws, "Iron Teeth (Sympathetic Strike)", Trait.Brawling)
			.WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Piercing)).WithSoundEffect(SfxName.BiteApple)
			.WithAdditionalWeaponProperties(property => property.WithOnTarget(ApplySympatheticLink));
		var teethFeat = new TrueFeat(ModManager.RegisterFeatName("WitchArmamentsTeeth", "Witch's Armaments (Iron Teeth)"), 1,
				"Your patron’s power changes your body to ensure you are never defenseless.",
				"With a click of your jaw, your teeth transform into long metallic points. You gain a jaws unarmed attack that deals 1d8 piercing damage and is in the brawling group.",
				[WitchLoader.TWitch, TSympStrike])
			.WithTag(teethSymp)
			.WithOnCreature(creature =>
			{
				creature.WithAdditionalUnarmedStrike(teeth);
			});
		yield return teethFeat;

		var hair = new Item(IllustrationName.BlackTentacles, "Living Hair", Trait.Brawling, Trait.Agile,
				Trait.Disarm, Trait.Finesse, Trait.Trip, Trait.Unarmed)
			.WithWeaponProperties(new WeaponProperties("1d4", DamageKind.Bludgeoning))
			.WithSoundEffect(SfxName.BiteApple); // TODO: sound effect
		var hairSymp = new Item(IllustrationName.BlackTentacles, "Living Hair (Sympathetic Strike)", Trait.Brawling, Trait.Agile,
				Trait.Disarm, Trait.Finesse, Trait.Trip, Trait.Unarmed)
			.WithWeaponProperties(new WeaponProperties("1d4", DamageKind.Bludgeoning))
			.WithSoundEffect(SfxName.BiteApple) // TODO: sound effect
			.WithAdditionalWeaponProperties(property => property.WithOnTarget(ApplySympatheticLink));
		var hairFeat =  new TrueFeat(ModManager.RegisterFeatName("WitchArmamentsHair", "Witch's Armaments (Living Hair)"), 1,
				"Your patron’s power changes your body to ensure you are never defenseless.",
				"You can instantly grow or shrink your hair, eyebrows, beard, or mustache by up to several feet and manipulate your hair for use as a weapon, though your control isn’t fine enough for more dexterous tasks. You gain a hair unarmed attack that deals 1d4 bludgeoning damage; is in the brawling group; and has the agile, disarm, finesse, trip, and unarmed traits.",
				[WitchLoader.TWitch, TSympStrike])
			.WithTag(hairSymp)
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
				
				var armamentFeats = calculated.AllFeats.FindAll(feat => feat.HasTrait(TSympStrike));

				foreach (var armamentFeat in armamentFeats)
				{
					if (armamentFeat.Tag is not Item armament)
						continue;

					witch.AddQEffect(new QEffect
					{
						AdditionalUnarmedStrike = armament
					});
				}
			});

		// yield return new TrueFeat(ModManager.RegisterFeatName("WitchBottle", "Witch's Bottle"), 8,
		// 	"",
		// 	"",
		// 	[WitchLoader.TWitch])
		// 	.WithPrerequisite(cauldron, "Cauldron");
	}

	private static async Task ApplySympatheticLink(CombatAction spell, Creature caster, Creature target, CheckResult result)
	{
		caster.AddQEffect(QSympStrikeUsed);
		switch (result)
		{
			case < CheckResult.Success:
				return;
			case CheckResult.Success:
				target.AddQEffect(new QEffect("Sympathetic Link", "You take a –1 circumstance penalty to saves against hexes", ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster) { BonusToSpellSaveDCsForSpecificSpell = (effect, action) => action.HasTrait(WitchSpells.THex) ? new Bonus(-1, BonusType.Circumstance, "Sympathetic Link") : null });
				break;
			case CheckResult.CriticalSuccess:
				target.AddQEffect(new QEffect("Sympathetic Link", "You take a –2 circumstance penalty to saves against hexes", ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster) { BonusToSpellSaveDCsForSpecificSpell = (effect, action) => action.HasTrait(WitchSpells.THex) ? new Bonus(-2, BonusType.Circumstance, "Sympathetic Link") : null });
				break;
		}
	}
}