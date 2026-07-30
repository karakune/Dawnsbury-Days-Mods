using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.DeployableFamiliars;

public static class FamiliarFeats
{
	public static void Load()
	{

		// Update feats after they've been created.
		LoadOrder.WhenFeatsBecomeLoaded += () =>
		{
			// Update existing feats without breaking other mods
			MakeAllFamiliarsDeployable();
			
			foreach (Feat ft in AllFeats.All)
			{
				// Organize the familiar abilities into appropriate categories, starting with the pre-existing feats.
				if (ft.HasTrait(Trait.CombatFamiliarAbility))
					ft.FeatGroup ??= ModData.FeatGroups.FamiliarAbilities; // Check for null to avoid overwriting our own feats.
				
				// Hide the illustration while familiar is deployed, restore if not.
				if (ft.HasTrait(Trait.FamiliarIllustrationDisplay))
				{
					// This will execute after the previous OnSheet, ensuring that
					// the illustration info is available to overwrite.
					ft.WithOnCreature(self =>
					{
						// Capture data, do not change
						// End if for any reason, we don't have a corner illustration
						if (self.Illustration is not CornerIllustration(var me, var familiar, var dir))
							return;

						self.AddQEffect(new QEffect()
						{
							// An ID without going all Registration about it
							Name = "[FAMILIAR CORNER ILLUSTRATION MANAGER]",
							StateCheck = qfThis =>
							{
								CornerIllustration? cornerIllCur = qfThis.Owner.Illustration as CornerIllustration;
								bool shouldHideFamiliar = qfThis.Owner.HasEffect(ModData.QEffectIds.FamiliarDeployed) || DeployableFamiliarTag.IsFamiliarDead(qfThis.Owner);
								if (shouldHideFamiliar)
								{
									if (cornerIllCur is not null) // Only need to do this once
										qfThis.Owner.Illustration = me;
								}
								else
								{
									if (cornerIllCur is null) // Only need to do this once
										qfThis.Owner.Illustration = new CornerIllustration(me, familiar, dir);
								}
							}
						});
					});
				}
			}
		};

		foreach (Feat feat in CreateFeats())
			ModManager.AddFeat(feat);
		
		foreach (Feat feat in CreateClassFeats())
			ModManager.AddFeat(feat);
	}
	
	/// <summary>
	/// Takes known familiar feats and converts them into deployable familiars.
	/// </summary>
	public static void MakeAllFamiliarsDeployable()
	{
		List<FeatName> baseGameFeats = [
			FeatName.ClassFamiliar,
			FeatName.AnimalAccomplice,
			// Add modded feats specifically designated as compatible with becoming a Deployable Familiar. 
			..AllFeats.All
				.Where(ft => ft.HasTrait(ModData.Traits.DeployableFamiliarFeat))
				.Select(ft => ft.FeatName)
		];
		
		foreach (FeatName ft in baseGameFeats)
			MakeDeployable(ft);
	}

	/// <summary>
	/// Takes an existing feat made using <see cref="Familiars.CreateFamiliarFeat(FeatName, Trait[])"/> and turns it into a deployable familiar.
	/// </summary>
	/// <param name="familiarFeat">A familiar-granting feat such as <see cref="FeatName.ClassFamiliar"/>.</param>
	/// <returns></returns>
	public static Feat MakeDeployable(FeatName familiarFeat)
	{
		Feat modifiedFeat = AllFeats.GetFeatByFeatName(familiarFeat)
			// TODO: This is going to be part of the system that deals with specific familiars
			// From "new FamiliarFeat()"
			/*.WithOnSheet(sheet =>
			{
				foreach (FeatName innateFeat in innateFeatNames)
					sheet.GrantFeat(innateFeat);
			})*/
			.WithOnSheet(values =>
			{
				// Get original tag
				if (values.Tags.GetValueOrDefault(Familiars.FAMILIAR_KEY) is not FamiliarTag fTag)
					return;

				// Replace with new tag
				DeployableFamiliarTag dFTag = new DeployableFamiliarTag()
				{
					FamiliarName = fTag.FamiliarName,
					FamiliarAbilities = fTag.FamiliarAbilities,
					Illustration = fTag.Illustration
				};
				values.Tags[Familiars.FAMILIAR_KEY] = dFTag;

				// Deployment Toggle
				values.AddSelectionOption(new SingleFeatSelectionOption(
					"PrecombatDeploy",
					"Deploy familiar?",
					-1,
					feat => feat.HasTrait(ModData.Traits.FamiliarDeploy))
				{
					OptionLevel = SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL
				});
			})
			.WithOnCreature(self =>
			{
				if (DeployableFamiliarTag.FindTag(self) is null
				    || DeployableFamiliarTag.IsFamiliarDead(self))
					return;

				QEffect commandGranter = new QEffect
				{
					// This ID is used for familiar actions in the base game, the same limitation as commanding a familiar once per turn.
					Id = QEffectId.FamiliarAbility,
					UsedThisTurn = false, // When this effect is used, you cannot use a familiar command action again.
					ProvideMainAction = qfThis =>
					{
						if (DeployableFamiliarTag.FindTag(qfThis.Owner) is not { } fTag
						    || DeployableFamiliarTag.IsFamiliarDead(self))
							return null;
						CombatAction deployment = DeployableFamiliarTag.FindFamiliar(qfThis.Owner) is not null
							? CreateRetrieveFamiliarAction(qfThis.Owner, fTag)
							: CreateDeployFamiliarAction(qfThis.Owner, fTag);

						List<Possibility> commandPossibilities =
						[
							(ActionPossibility)CreateCommandFamiliarAction(qfThis.Owner,
								DeployableFamiliarTag.FindFamiliar(qfThis.Owner), fTag),
						];
						if (qfThis.Owner.HasFeat(ModData.FeatNames.Independent))
							commandPossibilities.Add(
								(ActionPossibility)CreateIndependentFamiliarAction(qfThis.Owner,
									DeployableFamiliarTag.FindFamiliar(qfThis.Owner), fTag));
						
						return new SubmenuPossibility(
								fTag.IllustrationOrDefault,
								fTag.FamiliarName ?? "Familiar")
							{
								Subsections =
								{
									new PossibilitySection("Command Familiar")
									{
										Possibilities = commandPossibilities
									},
									new PossibilitySection("Familiar action")
									{
										PossibilitySectionId = PossibilitySectionId.FamiliarAbility,
										Possibilities = []
									},
									new PossibilitySection("Deployment")
									{
										Possibilities = [
											(ActionPossibility)deployment
										]
									}
								}
							}
							.WithPossibilityGroup("Abilities");
					},
					// Automates the consumption of your familiar command.
					AfterYouTakeAction = async (qfThis, action) =>
					{
						if (!FamiliarAbilities.IsFamiliarAction(action))
							return;
						qfThis.UsedThisTurn = true;
					}
				};
				// Has to be done separate due to lacking a QEffect self-reference.
				// Keeps you from using commands more than once.
				// Applies other restrictions to familiar abilities.
				commandGranter.PreventTakingAction = action =>
				{
					// Don't care if we're not dealing with familiar actions
					if (!FamiliarAbilities.IsFamiliarAction(action))
						return null;

					return ModData.CommonRequirements.WhyCannotCommand(
						action.Owner,
						action.ActionId == ModData.ActionIds.CommandFamiliar,
						action.Name.ToLower() is
							"item delivery" or "lab assistant" or "restorative familiar" or "valet");
				};
				self.AddQEffect(commandGranter);
			});
		modifiedFeat.RulesText += "\n\n{b}Deployable Familiars{/b} This familiar can be deployed in combat as a creature, gaining extra mechanics compared to a normal combat familiar.";
		return modifiedFeat;
	}
	
	public static IEnumerable<Feat> CreateFeats()
	{
		yield return new Feat(
				ModData.FeatNames.AutoDeployNo,
				"", "Your familiar won't be deployed until you take an action to do so.",
				[ModData.Traits.FamiliarDeploy],
				null);
		
		yield return new Feat(
				ModData.FeatNames.AutoDeployYes,
				"", "Your familiar will automatically be deployed at the start of combat in the nearest space to you.",
				[ModData.Traits.FamiliarDeploy],
				null)
			.WithOnCreature(owner =>
				owner.AddQEffect(new QEffect()
				{
					StartOfCombat = async qfThis =>
					{
						if (DeployableFamiliarTag.FindTag(qfThis.Owner) is not {} fTag
						    || DeployableFamiliarTag.IsFamiliarDead(qfThis.Owner))
							return;
						
						fTag.Spawn(qfThis.Owner);
						qfThis.Owner.AddQEffect(new QEffect { Id = ModData.QEffectIds.FamiliarDeployed });
					}
				}));
		
		// TODO: Figure out specific familiars
		/*yield return CreateFamiliarFeat("Cauldron", ModData.Illustrations.FamiliarCauldron, [FamiliarAbilities.FNTough, FamiliarAbilities.FNConstruct]);
		yield return CreateFamiliarFeat("Crow", ModData.Illustrations.FamiliarCrow, [FamiliarAbilities.FNFlier]);
		yield return CreateFamiliarFeat("Frog", ModData.Illustrations.FamiliarFrog, [FamiliarAbilities.FNAmphibious]);
		yield return CreateFamiliarFeat("Leshy", IllustrationName.WoundedLeshy, [FamiliarAbilities.FNPlant]);
		yield return CreateFamiliarFeat("Snake", IllustrationName.AnimalFormSnake, []);*/
	}
	
	public static IEnumerable<Feat> CreateClassFeats()
	{
		yield return new Feat(ModData.FeatNames.ArcaneThesisImprovedFamiliar,
				"Your thesis is 'Familiars: An extensive study of the benefits of pets'.",
				"You gain the Familiar wizard feat. Your familiar gains an extra ability, and it gains an additional extra ability when you reach 6th, 12th, and 18th levels.",
				[Trait.ArcaneThesis], null)
			.WithOnSheet(values =>
			{
				values.GrantFeat(FeatName.ClassFamiliar);
				IncreaseByOne(values);
				foreach (int level in (int[])[6, 12, 18])
					values.AddAtLevel(level, IncreaseByOne);
				
				return;

				void IncreaseByOne(CalculatedCharacterSheetValues values2)
				{
					// TODO: This might be bugged? Like it might not be available in time.
					if (DeployableFamiliarTag.FindTag(values2) is not { } fTag2) 
						return;
					fTag2.FamiliarAbilities++;
				}
			});
	}

	public static CombatAction CreateRetrieveFamiliarAction(Creature owner, DeployableFamiliarTag fTag)
	{
		return new CombatAction(
				owner,
				fTag.IllustrationOrDefault,
				"Retrieve Familiar",
				[ModData.ModTrait, Trait.Basic, Trait.Concentrate],
				$$"""
				{b}Frequency{/b} once per turn

				Command {Blue}{{fTag.FamiliarName ?? "Familiar"}}{/Blue}. If it ends any action adjacent to you, you will retrieve it from the battlefield.
				""",
				Target.Self()
					.WithAdditionalRestriction(self => DeployableFamiliarTag.FindFamiliar(self) is null
						? "No familiar"
						: null))
			.WithActionCost(1)
			.WithActionId(ModData.ActionIds.RetrieveFamiliar)
			.WithEffectOnEachTarget(async (_, caster, _, _) =>
			{
				if (DeployableFamiliarTag.FindFamiliar(caster) is not { } familiar)
					return;

				familiar.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
				{
					AfterYouTakeAction = async (qfThis, anyAction) =>
					{
						if (qfThis.Owner.DistanceTo(caster) <= 1)
							caster.Battle.RemoveCreatureFromGame(familiar);
					}
				});
				familiar.Actions.AnimateActionUsedTo(0, ActionDisplayStyle.Slowed);
				familiar.Actions.ActionsLeft = 2;
				await CommonSpellEffects.YourMinionActs(familiar);
			});
	}

	public static CombatAction CreateDeployFamiliarAction(Creature owner, DeployableFamiliarTag fTag)
	{
		return new CombatAction(
				owner,
				fTag.IllustrationOrDefault,
				"Deploy Familiar",
				[ModData.ModTrait, Trait.Basic, Trait.Concentrate],
				$$"""
				  {b}Frequency{/b} once per turn

				  Command {Blue}{{fTag.FamiliarName ?? "Familiar"}}{/Blue} to deploy onto the battlefield. It will have 1 action remaining afterwards.
				  """,
				Target.RangedEmptyTileForSummoning(1)
					.WithAdditionalSelfRequirement(self =>
					{
						if (DeployableFamiliarTag.IsFamiliarDead(self))
							return Usability.NotUsable("Familiar is dead");
						if (DeployableFamiliarTag.FindFamiliar(self) is not null)
							return Usability.NotUsable("Already deployed");
						return Usability.Usable;
					}))
			.WithActionCost(1)
			.WithActionId(ModData.ActionIds.DeployFamiliar)
			.WithEffectOnEachTile(async (_, self, tiles) =>
			{
				fTag.Spawn(owner, tiles.FirstOrDefault());
				owner.AddQEffect(new QEffect { Id = ModData.QEffectIds.FamiliarDeployed });
				if (DeployableFamiliarTag.FindFamiliar(self) is { } familiar)
				{
					familiar.Actions.AnimateActionUsedTo(0, ActionDisplayStyle.Slowed);
					familiar.Actions.AnimateActionUsedTo(1, ActionDisplayStyle.UsedUp);
					familiar.Actions.AnimateActionUsedTo(2, ActionDisplayStyle.Available);
					familiar.Actions.ActionsLeft = 1;
					await CommonSpellEffects.YourMinionActs(familiar);
				}
			});
	}

	public static CombatAction CreateCommandFamiliarAction(
		Creature owner,
		Creature? familiar,
		DeployableFamiliarTag fTag)
	{
		return new CombatAction(
				owner,
				fTag.IllustrationOrDefault,
				"Command Familiar",
				[ModData.ModTrait, Trait.Basic, Trait.Auditory, Trait.Concentrate],
				$$"""
				{i}You issue your familiar a command.{/i}

				{b}Frequency{/b} once per turn

				Take 2 actions as {Blue}{{familiar?.Name ?? fTag.FamiliarName ?? "Familiar"}}{/Blue}.
				""",
				Target.Self()
					.WithAdditionalRestriction(self =>
						ModData.CommonRequirements.WhyCannotCommand(self, true)))
			.WithActionCost(1)
			.WithActionId(ModData.ActionIds.CommandFamiliar)
			.WithEffectOnEachTarget(async (_, _, _, _) =>
			{
				if (familiar is null)
					return;
				familiar.Actions.AnimateActionUsedTo(0, ActionDisplayStyle.Slowed);
				familiar.Actions.ActionsLeft = 2;
				await CommonSpellEffects.YourMinionActs(familiar);
			});
	}

	public static CombatAction CreateIndependentFamiliarAction(
		Creature owner,
		Creature? familiar,
		DeployableFamiliarTag fTag)
	{
		return new CombatAction(
				owner,
				fTag.IllustrationOrDefault,
				"Independent Familiar",
				[ModData.ModTrait, Trait.Basic, Trait.Auditory, Trait.Concentrate],
				$$"""
				  {i}Your familiar takes an action on its own.{/i}

				  {b}Frequency{/b} once per turn

				  Take 1 action as {Blue}{{familiar?.Name ?? fTag.FamiliarName ?? "Familiar"}}{/Blue}.
				  """,
				Target.Self()
					.WithAdditionalRestriction(self =>
						ModData.CommonRequirements.WhyCannotCommand(self, true)))
			.WithActionCost(0)
			.WithActionId(ModData.ActionIds.CommandFamiliar)
			.WithEffectOnEachTarget(async (_, _, _, _) =>
			{
				if (familiar is null)
					return;
				familiar.Actions.AnimateActionUsedTo(0, ActionDisplayStyle.Slowed);
				familiar.Actions.AnimateActionUsedTo(1, ActionDisplayStyle.Slowed);
				familiar.Actions.ActionsLeft = 1;
				await CommonSpellEffects.YourMinionActs(familiar);
			});
	}
}