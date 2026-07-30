using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Possibilities;
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
		
		// Echolocation
		yield return DeployableFamiliarAbility(
				ModData.FeatNames.Echolocation,
				ModData.FeatGroups.FamiliarAbilities,
				null,
				"Your familiar gains a precise sense with a range of 20 feet, which means that creatures can't be hidden within the area while it is conscious, unless it is deafened.",
				innate =>
				{
					innate.Description =
						"You observe creatures within 20 feet using your precise sense, unless you are deafened.";
				},
				familiar =>
				{
					familiar.AddQEffect(PreciseEcholocation());
					DeployableFamiliarTag.FindMaster(familiar)?.FindQEffect(ModData.QEffectIds.FamiliarEcholocation)
						?.ExpiresAt = ExpirationCondition.Ephemeral;
				})
			.WithOnCreature(self =>
			{
				self.AddQEffect(PreciseEcholocation());
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
		
		// Independent
		yield return DeployableFamiliarAbility(
			ModData.FeatNames.Independent,
			ModData.FeatGroups.FamiliarAbilities,
			null,
			"As a free action, you may command your familiar to take one action instead of commanding it normally.",
			innate =>
			{
				innate.Description = "You can take one action during your master's turn if they haven't commanded you.";
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
			masterAbility = masterAbility.WithPrerequisite(
				values => values.HasFeat(witchSubclassPrerequisite.Value),
				$"You must be a witch with the {Feat.ToDisplayName(witchSubclassPrerequisite.Value)} patron.");
		masterAbility.FeatGroup = featGroup;
		return masterAbility;
	}
	
	/// <summary>
	/// Determines if an action is a familiar action. Searches through your possibilities; is a familiar action if it's found in a SubmenuPossibility with a PossibilitySection named "Familiar action". Includes whitelist inclusion of the CommandFamiliar, DeployFamiliar, and RetrieveFamiliar actions.
	/// </summary>
	public static bool IsFamiliarAction(CombatAction familiarAction)
	{
		// If not whitelisted, would never be considered a familiar action.
		if (familiarAction.ActionId == ModData.ActionIds.CommandFamiliar
		    || familiarAction.ActionId == ModData.ActionIds.DeployFamiliar
		    || familiarAction.ActionId == ModData.ActionIds.RetrieveFamiliar)
			return true;
		
		// Avoid loading crashes
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (familiarAction.Owner.Possibilities is null)
			return false;
		
		SubmenuPossibility? familiarMenu = LookInPossibilities(
		    familiarAction.Owner.Possibilities,
		    poss =>
		        poss is SubmenuPossibility submenu
		        && submenu.Subsections.Any(sect =>
			        sect.Name.Contains("Familiar action")));

		var familiarActions = familiarMenu?.Subsections
			.FirstOrDefault(sect =>
				sect.Name.Contains("Familiar action"));

		return familiarActions?.Filter(ap => ap.CombatAction.Name == familiarAction.Name)?.ActionCount > 0;

		SubmenuPossibility? LookInPossibilities(
		    Possibilities posses,
		    Func<Possibility, bool> keepOnlyWhat)
		{
		    foreach (PossibilitySection section in posses.Sections)
		    {
		        SubmenuPossibility? submenu = LookInSection(section, keepOnlyWhat);
		        if (submenu != null)
		            return submenu;
		    }

		    return null;
		}

		SubmenuPossibility? LookInSection(
		    PossibilitySection section,
		    Func<Possibility, bool> keepOnlyWhat)
		{
		    foreach (Possibility possibility in section.Possibilities)
		    {
		        if (possibility is not SubmenuPossibility submenu)
		            continue;
		        if (keepOnlyWhat(submenu) || LookInMenu(submenu, keepOnlyWhat) is not null)
		            return submenu;
		    }

		    return null;
		}

		SubmenuPossibility? LookInMenu(
		    SubmenuPossibility submenu,
		    Func<Possibility, bool> keepOnlyWhat)
		{
		    foreach (PossibilitySection section in submenu.Subsections)
		    {
		        SubmenuPossibility? submenuInner = LookInSection(section, keepOnlyWhat);
		        if (submenuInner != null)
		            return submenuInner;
		    }

		    return null;
		}
	}
    
    /// <summary>
    /// Creates an innate QEffect for echolocation precise sense.
    /// </summary>
    /// <returns></returns>
    public static QEffect PreciseEcholocation()
    {
        return new QEffect("Echolocation", "You observe creatures within 20 feet using your precise sense, unless deafened.")
        {
	        Id = ModData.QEffectIds.FamiliarEcholocation,
            Tag = 4,
            StateCheck = qfThis =>
            {
                if (qfThis.Owner.HasEffect(QEffectId.Unconscious) || qfThis.Owner.HasEffect(QEffectId.Deafened))
                    return;

                if (qfThis.Owner.HasEffect(ModData.QEffectIds.YourFamiliarIsDead))
	                return;
                
                int innerRange = (int)qfThis.Tag!;
                qfThis.Owner.Battle.AllCreatures
                    .Where(cr =>
                        cr.EnemyOf(qfThis.Owner) && cr.DistanceTo(qfThis.Owner) <= innerRange)
                    .ForEach(cr =>
                    {
	                    cr.DetectionStatus.Undetected = false;
	                    cr.DetectionStatus.HiddenTo.Remove(qfThis.Owner);
                    });
            },
        };
    }
}