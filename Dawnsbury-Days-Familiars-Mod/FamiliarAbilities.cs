using System;
using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Familiars;

public static class FamiliarAbilities
{
	public static Trait TFamiliarAbility = ModManager.RegisterTrait("FamiliarAbility");
	public static Trait TFamiliarAbilitySelection = ModManager.RegisterTrait("FamiliarAbilitySelection");

	public static FeatName FNAmphibious = ModManager.RegisterFeatName("FamAmphibiousSelection", "Amphibious");
	public static FeatName FNConstruct = ModManager.RegisterFeatName("FamConstructSelection", "Construct");
	public static FeatName FNDragon = ModManager.RegisterFeatName("FamDragonSelection", "Dragon");
	public static FeatName FNFastMov = ModManager.RegisterFeatName("FamFastMovSelection", "Fast Movement");
	public static FeatName FNFlier = ModManager.RegisterFeatName("FamFlierSelection", "Flier");
	public static FeatName FNPlant = ModManager.RegisterFeatName("FamPlantSelection", "Plant");
	public static FeatName FNTough = ModManager.RegisterFeatName("FamToughSelection", "Tough");
	
	public static IEnumerable<Feat> CreateFeats()
	{
		Feat[][] feats =
		[
			RegisterFamiliarAbility(FNAmphibious, "FamAmphibious", "Amphibious", 
				"Your familiar gains the Amphibious trait, giving it swimming.", 
				creature => { 
					creature.Traits.Add(Trait.Amphibious);
					creature.AddQEffect(QEffect.Swimming());
				}),
			
			RegisterFamiliarAbility(FNConstruct, "FamConstruct", "Construct", 
				"Your familiar has the construct trait instead of the animal trait."/* Your familiar must have the tough familiar ability to select this."*/, 
				creature => { 
					creature.Traits.Remove(Trait.Animal);
					creature.Traits.Add(Trait.Construct);
					CommonEnvironmentActions.BecomeConstruct(creature);
				}/*, [(FNTough, "Tough")]*/),
			
			RegisterFamiliarAbility(FNDragon, "FamDragon", "Dragon",
				"Your familiar has the dragon trait instead of the animal trait.",
				creature =>
				{
					creature.Traits.Remove(Trait.Animal);
					creature.Traits.Add(Trait.Dragon);
				}),

			RegisterFamiliarAbility(FNFastMov, "FamFastMov", "Fast Movement",
				"Your familiar's speed becomes 40 feet.",
				creature => creature.BaseSpeed = 8),

			RegisterFamiliarAbility(FNFlier, "FamFlier", "Flier",
				"Your familiar gains a fly speed of 25 feet.",
				creature => creature.AddQEffect(QEffect.Flying())),
		
			RegisterFamiliarAbility(FNPlant, "FamPlant", "Plant",
			"Your familiar has the plant trait instead of the animal trait.",
			creature =>
				{
					creature.Traits.Remove(Trait.Animal);
					creature.Traits.Add(Trait.Plant);
				}),

			RegisterFamiliarAbility(FNTough, "FamTough", "Tough",
				"Your familiar's Max HP increase by 2 per level.",
				creature => creature.MaxHP += 2 * creature.Level),
		];
		
		foreach (var duo in feats)
			foreach (var feat in duo)
				yield return feat;
	}

	public static Feat[] RegisterFamiliarAbility(FeatName selectionFeatName, string technicalName, string displayName, string rulesText,
		Action<Creature> effectOnFamiliar, List<(FeatName, string)>? prerequisiteSelectionFeats = null)
	{
		var familiarFeat = new Feat(ModManager.RegisterFeatName(technicalName, displayName), null, rulesText,
				[TFamiliarAbility], null)
			.WithOnCreature(effectOnFamiliar)
			.WithOnCreature(creature => creature.AddQEffect(new QEffect(displayName, "")));
		
		var selectionFeat = new Feat(selectionFeatName, null,
			rulesText, [TFamiliarAbilitySelection], null)
			.WithOnCreature(owner =>
				owner.AddQEffect(new QEffect
				{
					StartOfCombat = async effect =>
					{
						var master = effect.Owner;
						var familiar = Familiar.GetFamiliar(master);
						familiar?.WithFeat(familiarFeat.FeatName);
					}
				}));

		if (prerequisiteSelectionFeats != null) 
			selectionFeat = prerequisiteSelectionFeats.Aggregate(selectionFeat, 
				(current, feat) => current.WithPrerequisite(
					sheet => sheet.HasFeat(feat.Item1), $"Your familiar must have the {feat.Item2} ability."));

		return [familiarFeat, selectionFeat];
	}
}