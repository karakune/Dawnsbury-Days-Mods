using System;
using System.Collections.Generic;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Familiars;

public static class FamiliarAbilities
{
	public static Trait TFamiliarAbility = ModManager.RegisterTrait("FamiliarAbility");
	public static Trait TFamiliarAbilitySelection = ModManager.RegisterTrait("FamiliarAbilitySelection");
	public static IEnumerable<Feat> CreateFeats()
	{
		Feat[][] feats =
		[
			RegisterFamiliarAbility("FamAmphibious", "Amphibious", 
				"Your familiar gains the Amphibious trait, giving it swimming.", 
				creature => { 
					creature.Traits.Add(Trait.Amphibious);
					creature.AddQEffect(QEffect.Swimming());
				}),
			
			RegisterFamiliarAbility("FamDragon", "Dragon",
				"Your familiar has the dragon trait instead of the animal trait.",
				creature =>
				{
					creature.Traits.Remove(Trait.Animal);
					creature.Traits.Add(Trait.Dragon);
				}),

			RegisterFamiliarAbility("FamFastMov", "Fast Movement",
				"Your familiar's speed becomes 40 feet.",
				creature => creature.BaseSpeed = 8),

			RegisterFamiliarAbility("FamFlier", "Flier",
				"Your familiar gains a fly speed of 25 feet.",
				creature => creature.AddQEffect(QEffect.Flying())),
		
			RegisterFamiliarAbility("FamPlant", "Plant",
			"Your familiar has the plant trait instead of the animal trait.",
			creature =>
				{
					creature.Traits.Remove(Trait.Animal);
					creature.Traits.Add(Trait.Plant);
				}),

			RegisterFamiliarAbility("FamTough", "Tough",
				"Your familiar's Max HP increase by 2 per level.",
				creature => creature.MaxHP += 2 * creature.Level),
		];
		
		foreach (var duo in feats)
			foreach (var feat in duo)
				yield return feat;
	}

	private static Feat[] RegisterFamiliarAbility(string technicalName, string displayName, string rulesText,
		Action<Creature> effectOnFamiliar)
	{
		var familiarFeat = new Feat(ModManager.RegisterFeatName(technicalName, displayName), null, rulesText,
				[TFamiliarAbility], null)
			.WithOnCreature(effectOnFamiliar)
			.WithOnCreature(creature => creature.AddQEffect(new QEffect(displayName, "")));
		
		var selectionFeat = new Feat(ModManager.RegisterFeatName($"{technicalName}Selection", displayName), null,
			rulesText, [TFamiliarAbilitySelection], null)
			.WithOnCreature(owner =>
				owner.AddQEffect(new QEffect($"Apply{technicalName}Selection", "Apply familiar abilities")
				{
					StartOfCombat = async effect =>
					{
						var master = effect.Owner;
						var familiar = Familiar.GetFamiliar(master);
						familiar?.WithFeat(familiarFeat.FeatName);
					}
				}));

		return [familiarFeat, selectionFeat];
	}
}