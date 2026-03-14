using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.DeployableFamiliars;

public static class FamiliarAbilities
{
	public static void Load()
	{
		foreach (Feat ft in CreateFeats())
			ModManager.AddFeat(ft);
	}

	public static IEnumerable<Feat> CreateFeats()
	{
		// Amphibious
		yield return DeployableFamiliarAbility(
			ModData.FeatNames.Amphibious,
			ModData.FeatGroups.FamiliarAbilities,
			null,
			"Your familiar gains the amphibious trait and swimming {i}(it can move across water as though it were solid ground){/i}.",
			innate =>
			{
				innate.Description = "You gain the amphibious trait and swimming.";
				innate.Id = QEffectId.Swimming;
			},
			familiar => familiar.WithExtraTrait(Trait.Amphibious));
		
		// Construct
		// BUG: Doesn't work because prereqs don't evaluate properly.
		yield return DeployableFamiliarAbility(
				ModData.FeatNames.Construct,
				ModData.FeatGroups.FamiliarAbilities,
				null,
				"Your familiar has the construct trait instead of the animal trait. The familiar is immune to death effects, disease, doomed, drained, fatigued, healing, nonlethal attacks, paralyzed, poison, sickened, spirit, unconscious, vitality, and void.",
				innate => innate.Description = "You have the construct trait instead of the animal trait.",
				familiar =>
				{
					familiar.Traits.Remove(Trait.Animal);
					familiar.WithExtraTrait(Trait.Construct);
					CommonEnvironmentActions.BecomeConstruct(familiar);
				})
			.WithPrerequisite(
				values => values.HasFeat(ModData.FeatNames.Tough),
				"Your familiar must have the Tough ability.");
		
		// Dragon
		yield return DeployableFamiliarAbility(
			ModData.FeatNames.Dragon,
			ModData.FeatGroups.FamiliarAbilities,
			null,
			"Your familiar has the dragon trait instead of the animal trait.",
			innate => innate.Description = "You have the dragon trait instead of the animal trait.",
			familiar =>
			{
				familiar.Traits.Remove(Trait.Animal);
				familiar.WithExtraTrait(Trait.Dragon);
			});

		// Fast Movement
		yield return DeployableFamiliarAbility(
			ModData.FeatNames.FastMovement,
			ModData.FeatGroups.FamiliarAbilities,
			null,
			"Your familiar's Speed increases to 40 feet.",
			innate => innate.Description = "Your Speed increases to 40 feet.",
			familiar => familiar.BaseSpeed = Math.Max(familiar.BaseSpeed, 8));
		
		// Flier
		yield return DeployableFamiliarAbility(
			ModData.FeatNames.Flier,
			ModData.FeatGroups.FamiliarAbilities,
			null,
			"Your familiar gains flying {i}(it ignores difficult and hazardous terrain and can move over water, lava and chasms){/i}.",
			innate =>
			{
				innate.Description = "You gain flying.";
				innate.Id = QEffectId.Flying;
			});
		
		// Manual Dexterity
		yield return DeployableFamiliarAbility(
			ModData.FeatNames.ManualDexterity,
			ModData.FeatGroups.FamiliarAbilities,
			null,
			"Your familiar can hold items and perform manipulate actions.",
			innate =>
			{
				innate.Description = "You can hold items and take manipulate actions.";
				innate.Id = ModData.QEffectIds.FamiliarCanManipulate;
			});
		
		// Plant
		yield return DeployableFamiliarAbility(
			ModData.FeatNames.Plant,
			ModData.FeatGroups.FamiliarAbilities,
			null,
			"Your familiar has the plant trait instead of the animal trait.",
			innate => innate.Description = "You have the plant trait instead of the animal trait.",
			familiar =>
			{
				familiar.Traits.Remove(Trait.Animal);
				familiar.WithExtraTrait(Trait.Plant);
			});
		
		// Tough
		yield return DeployableFamiliarAbility(
			ModData.FeatNames.Tough,
			ModData.FeatGroups.FamiliarAbilities,
			null,
			"Your familiar's max HP increase by 2 per level.",
			innate => innate.Description = "Your max HP increases by 2 per level.",
			familiar => familiar.MaxHP += familiar.Level * 2);
	}

	/// <summary>
	/// Creates a familiar ability feat suited for Deployable Familiar functionality.
	/// </summary>
	/// <param name="featName">The <see cref="FeatName"/> of the familiar ability.</param>
	/// <param name="featGroup">One of the modded <see cref="ModData.FeatGroups"/>.</param>
	/// <param name="flavorText">The familiar ability's flavor text (if any).</param>
	/// <param name="rulesText">The rules text of the familiar ability.</param>
	/// <param name="modifyFamiliarInnate">The familiar also gets an innate QEffect from this ability, even if it's purely cosmetic. This is that QF to be modified. It always needs a description, but you can modify it further (such as to add <see cref="QEffectId.Swimming"/>).</param>
	/// <param name="onFamiliarSpawn">Optional actions to perform on the familiar when it spawns.</param>
	/// <param name="onSheet">Optional actions to perform on the feat-haver's sheet.</param>
	/// <param name="alternateIllustration">See: <see cref="Familiars.CreateFamiliarAbility"/>.</param>
	/// <returns></returns>
	public static Feat DeployableFamiliarAbility(
		FeatName featName,
		FeatGroup? featGroup,
		string? flavorText,
		string rulesText,
		Action<QEffect> modifyFamiliarInnate,
		Action<Creature>? onFamiliarSpawn = null,
		Action<CalculatedCharacterSheetValues>? onSheet = null,
		Illustration? alternateIllustration = null)
	{
		Feat familiarAbility = Familiars.CreateFamiliarAbility(featName, flavorText, rulesText, alternateIllustration)
			.WithPrerequisite(
				values => DeployableFamiliarTag.FindTag(values) is not null,
				"You must have a deployable familiar feat.")
			.WithOnSheet(values =>
			{
				if (DeployableFamiliarTag.FindTag(values) is not { } fTag)
					return;
				fTag.WithOnFamiliarSpawn(familiar =>
				{
					QEffect innate = new QEffect(
						Feat.ToDisplayName(featName),
						"[INNATE DESCRIPTION NOT SET]");
					modifyFamiliarInnate.Invoke(innate);
					familiar.AddQEffect(innate);
					onFamiliarSpawn?.Invoke(familiar);
				});
				onSheet?.Invoke(values);
			});
		familiarAbility.Traits.Insert(1, ModData.Traits.ModName);
		familiarAbility.FeatGroup = featGroup;
		return familiarAbility;
	}

	/// <summary>
	/// Creates a familiar ability feat suited for Deployable Familiar functionality.
	/// </summary>
	/// <param name="featName">The <see cref="FeatName"/> of the familiar ability.</param>
	/// <param name="featGroup">One of the modded <see cref="ModData.FeatGroups"/>.</param>
	/// <param name="flavorText">The familiar ability's flavor text (if any).</param>
	/// <param name="rulesText">The rules text of the familiar ability.</param>
	/// <param name="modifyMasterInnate">The master also gets an innate QEffect from this ability, even if it's purely cosmetic. This is that QF to be modified. It always needs a description, but you can modify it further (such as to add <see cref="QEffectId.Swimming"/>).</param>
	/// <param name="witchSubclassPrerequisite">Witch subclass prerequisite feat</param>
	/// <param name="alternateIllustration">See: <see cref="Familiars.CreateFamiliarAbility"/>.</param>
	/// <returns></returns>
	public static Feat DeployableMasterAbility(
		FeatName featName,
		FeatGroup? featGroup,
		string? flavorText,
		string rulesText,
		Action<QEffect> modifyMasterInnate,
		Illustration? alternateIllustration = null,
		FeatName? witchSubclassPrerequisite = null)
	{
		Feat masterAbility = Familiars.CreateFamiliarAbility(featName, flavorText, rulesText, alternateIllustration)
			.WithPrerequisite(
				values => DeployableFamiliarTag.FindTag(values) is not null,
				"You must have a deployable familiar feat.")
			.WithOnCreature((sheet, master) =>
			{
				QEffect innate = new QEffect(
					Feat.ToDisplayName(featName),
					rulesText);
				modifyMasterInnate.Invoke(innate);
				master.AddQEffect(innate);
			});
		if (witchSubclassPrerequisite != null)
			masterAbility = masterAbility.WithPrerequisite(witchSubclassPrerequisite.Value, Feat.ToDisplayName(witchSubclassPrerequisite.Value));
		masterAbility.Traits.Insert(1, ModData.Traits.ModName);
		masterAbility.FeatGroup = featGroup;
		return masterAbility;
	}
}