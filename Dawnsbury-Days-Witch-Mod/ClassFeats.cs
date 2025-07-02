using System.Collections.Generic;
using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
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

		// var cauldron = ModManager.RegisterFeatName("Cauldron");
		// yield return new TrueFeat(cauldron, 2,
		// 	"",
		// 	"",
		// 	[WitchLoader.TWitch]).WithOnSheet(values =>
		// {
		// });
		

		yield return new TrueFeat(ModManager.RegisterFeatName("WitchArmamentsNails", "Witch's Armaments (Eldritch Nails)"), 1,
				"Your patron’s power changes your body to ensure you are never defenseless.",
				"Your nails are supernaturally long and sharp. You gain a nails unarmed attack that deals 1d6 slashing damage, is in the brawling group, and has the agile and unarmed traits.",
				[WitchLoader.TWitch])
			.WithOnCreature(creature =>
			{
				creature.WithAdditionalUnarmedStrike(new Item(IllustrationName.DragonClaws, "Eldritch Claws", Trait.Brawling, Trait.Agile, Trait.Unarmed)
					.WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Slashing)).WithSoundEffect(SfxName.ScratchFlesh));
			});

		yield return new TrueFeat(ModManager.RegisterFeatName("WitchArmamentsTeeth", "Witch's Armaments (Iron Teeth)"), 1,
				"Your patron’s power changes your body to ensure you are never defenseless.",
				"With a click of your jaw, your teeth transform into long metallic points. You gain a jaws unarmed attack that deals 1d8 piercing damage and is in the brawling group.",
				[WitchLoader.TWitch])
			.WithOnCreature(creature =>
			{
				creature.WithAdditionalUnarmedStrike(new Item(IllustrationName.Jaws, "Iron Teeth", Trait.Brawling)
					.WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Piercing)).WithSoundEffect(SfxName.BiteApple));
			});

		yield return new TrueFeat(ModManager.RegisterFeatName("WitchArmamentsHair", "Witch's Armaments (Living Hair)"), 1,
				"Your patron’s power changes your body to ensure you are never defenseless.",
				"You can instantly grow or shrink your hair, eyebrows, beard, or mustache by up to several feet and manipulate your hair for use as a weapon, though your control isn’t fine enough for more dexterous tasks. You gain a hair unarmed attack that deals 1d4 bludgeoning damage; is in the brawling group; and has the agile, disarm, finesse, trip, and unarmed traits.",
				[WitchLoader.TWitch])
			.WithOnCreature(creature =>
			{
				creature.WithAdditionalUnarmedStrike(new Item(IllustrationName.BlackTentacles, "Living Hair", Trait.Brawling, Trait.Agile, Trait.Disarm, Trait.Finesse, Trait.Trip, Trait.Unarmed)
					.WithWeaponProperties(new WeaponProperties("1d4", DamageKind.Bludgeoning)).WithSoundEffect(SfxName.BiteApple));
			});

		// yield return new TrueFeat(ModManager.RegisterFeatName("WitchBottle", "Witch's Bottle"), 8,
		// 	"",
		// 	"",
		// 	[WitchLoader.TWitch])
		// 	.WithPrerequisite(cauldron, "Cauldron");
	}
}