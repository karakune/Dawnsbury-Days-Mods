using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Campaign.LongTerm;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.Familiars;

public static class FamiliarFeats
{
	public static Trait TFamiliar = ModManager.RegisterTrait("Familiar");
	public static Trait TFamiliarCommand = ModManager.RegisterTrait("FamiliarCommand", new TraitProperties("", relevant: false));
	public static Trait TFamiliarDeploy = ModManager.RegisterTrait("FamiliarDeploy", new TraitProperties("", relevant: false));
	public static QEffectId QHasFamiliar = ModManager.RegisterEnumMember<QEffectId>("HasFamiliar");

	public static FeatName FNWitchFamiliarBoost = ModManager.RegisterFeatName("WitchFamiliarBoost");
	
	public static IEnumerable<Feat> CreateFeats()
	{
		yield return new Feat(FNWitchFamiliarBoost, null, "", [], null);
		
		yield return new Feat(ModManager.RegisterFeatName("FamiliarAutoDeployNo", "No"), "", "", [TFamiliarDeploy], null)
			.WithOnCreature(owner => owner.AddQEffect(new QEffect("Familiar", "You have a familiar. You may deploy it onto the battlefied during your turn using a free action")
			{
				Id = QHasFamiliar,
				ProvideMainAction = effect =>
				{
					var master = effect.Owner;
					if (master.HasEffect(Familiar.QFamiliarDeployed))
						return null;
					
					var ill = Familiar.GetIllustration(master);
						
					var combatAction = new CombatAction(master, ill, "Deploy Familiar", [TFamiliar],
							"Deploy the familiar onto the battlefield.",
							Target.Self()
						)
						.WithActionCost(0)
						.WithEffectOnSelf(async _ =>
						{
							Familiar.Spawn(master);
							master.AddQEffect(new QEffect { Id = Familiar.QFamiliarDeployed});
						});
						
					return new ActionPossibility(combatAction);
				}
			}));
		yield return new Feat(ModManager.RegisterFeatName("FamiliarAutoDeployYes", "Yes"), "", "", [TFamiliarDeploy], null)
			.WithOnCreature(owner => owner.AddQEffect(new QEffect("Familiar", "You have a familiar")
			{
				Id = QHasFamiliar,
				StartOfCombat = async qf =>
				{
					Familiar.Spawn(qf.Owner);
					qf.Owner.AddQEffect(new QEffect { Id = Familiar.QFamiliarDeployed});
				}
			}));
		
		yield return CreateFamiliarFeat("Cauldron", Illustrations.FamiliarCauldron, [FamiliarAbilities.FNTough, FamiliarAbilities.FNConstruct]);
		yield return CreateFamiliarFeat("Crow", Illustrations.FamiliarCrow, [FamiliarAbilities.FNFlier]);
		yield return CreateFamiliarFeat("Frog", Illustrations.FamiliarFrog, [FamiliarAbilities.FNAmphibious]);
		yield return CreateFamiliarFeat("Leshy", IllustrationName.WoundedLeshy, [FamiliarAbilities.FNPlant]);
		yield return CreateFamiliarFeat("Snake", IllustrationName.AnimalFormSnake, []);
	}

	public static Feat CreateFamiliarFeat(string familiarKind, Illustration illustration, List<FeatName> innateFeatNames)
	{
		return new FamiliarFeat(familiarKind, innateFeatNames.Count)
			.WithOnSheet(sheet =>
			{
				foreach (var innateFeat in innateFeatNames)
				{
					sheet.GrantFeat(innateFeat);
				}

				sheet.Tags["FamiliarIllustration"] = illustration;
			})
			.WithOnCreature(owner =>
			{
				if (owner.HasEffect(Familiar.QDeadFamiliar))
					return;

				owner.AddQEffect(new QEffect
				{
					Traits = [TFamiliarCommand],
					ProvideMainAction = effect =>
					{
						var master = effect.Owner;
						var ill = Familiar.GetIllustration(master);
						var familiar = Familiar.GetFamiliar(master);
						
						var combatAction = new CombatAction(master, ill, "Command Familiar", [TFamiliar],
							"Take 2 actions as your familiar.\n\nYou can only command your familiar once per turn.",
							Target.Self()
							.WithAdditionalRestriction(_ => GetFamiliarCommandRestriction(master, familiar, isDirectCommand: true)))
							.WithActionCost(1)
							.WithEffectOnSelf(async _ =>
							{
								effect.UsedThisTurn = true;
								if (familiar == null)
									return;
								
								familiar.Actions.AnimateActionUsedTo(0, ActionDisplayStyle.Slowed);
								familiar.Actions.ActionsLeft = 2;
								await CommonSpellEffects.YourMinionActs(familiar);
							});
						
						return new ActionPossibility(combatAction);
					}
				});
			})
			.WithOnSheet(sheet =>
			{
				string defaultName = $"{sheet.Sheet.IdentityChoice?.Name}'s familiar";
				sheet.AddSelectionOption(new FreeTextSelectionOption("FamiliarNickname", "Familiar name", -1, $"You can name your familiar.\n\nIf you don't choose a name, it will be called {{b}}{defaultName}{{/b}}.", defaultName, 
					(v, sName) =>
					{
						v.Tags["FamiliarNickname"] = sName;
					}).WithIsOptional());
				
				sheet.AddSelectionOption(CreateFamiliarFeatsSelectionOption(sheet));
				sheet.AddSelectionOption(new SingleFeatSelectionOption("PrecombatDeploy", "Have familiar out at encounter start?", -1, feat => feat.HasTrait(TFamiliarDeploy))
				{
					OptionLevel = SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL
				});
			})
			.WithOnCreature(owner =>
			{
				owner.AddQEffect(new QEffect
				{
					StartOfCombat = async effect =>
					{
						var master = effect.Owner;
						var familiar = Familiar.GetFamiliar(master);
						if (familiar == null)
							return;

						var familiarName = Familiar.GetNickname(master);
						
						familiar.MainName = familiarName;
					}
				});
			});
	}

	public static string? GetFamiliarCommandRestriction(Creature master, Creature? familiar, bool isDirectCommand = false)
	{
		var hasBeenCommanded = master.QEffects.Any(e => e.Traits.Contains(TFamiliarCommand) && e.UsedThisTurn);
		if (hasBeenCommanded)
			return "You already commanded your familiar this turn.";

		if (familiar == null)
		{
			if (master.QEffects.Contains(Familiar.QDeadFamiliar))
				return "Your familiar is out of combat.";
			if (isDirectCommand)
				return "You must deploy your familiar to use this action.";
			return null;
		}

		if (familiar.HasEffect(QEffectId.Paralyzed))
			return "Your familiar is paralyzed.";
		if (familiar.HasEffect(QEffectId.Dying) || familiar.HasEffect(QEffectId.Unconscious))
			return "Your familiar is unconscious.";
		
		return null;
	}

	public static MultipleFeatSelectionOption CreateFamiliarFeatsSelectionOption(CalculatedCharacterSheetValues sheet)
	{
		var key = "FamiliarAbilities";
		var name = "Familiar Abilities";
		var level = -1; 
		var isImproved = sheet.HasFeat(FNWitchFamiliarBoost) || sheet.HasFeat(ClassFeats.FNArcaneThesisImpFam);
		
		var technicalMax = !isImproved ? 2 : sheet.CurrentLevel switch
		{
			< 6 => 3,
			< 12 => 4,
			< 18 => 5,
			_ => 6
		};

		if (sheet.HasFeat(ClassFeats.FNEnhancedFamiliar) || sheet.HasFeat(FamiliarMasterDedication.FNEnhancedFamiliarDedication))
			technicalMax += 2;

		if (sheet.HasFeat(ClassFeats.FNIncredibleFamiliar) || sheet.HasFeat(FamiliarMasterDedication.FNIncredibleFamiliarDedication))
			technicalMax += 2;

		var familiarFeat = sheet.AllFeats.OfType<FamiliarFeat>().FirstOrDefault();
		if (familiarFeat == null)
			return new MultipleFeatSelectionOption(key, name, level, _ => false, 0)
			{
				OptionLevel = SelectionOption.MORNING_PREPARATIONS_LEVEL
			};

		var finalMax = technicalMax - familiarFeat.InnateFeatsCount;
		
		return new MultipleFeatSelectionOption(key, name, level,
			(feat, _) => feat.HasTrait(FamiliarAbilities.TFamiliarAbilitySelection), finalMax)
		{
			OptionLevel = SelectionOption.MORNING_PREPARATIONS_LEVEL
		};
	}
}

public class FamiliarFeat : Feat
{
	public int InnateFeatsCount { get; }
	
	public FamiliarFeat(string familiarKind, int innateFeatsCount) 
		: base(ModManager.RegisterFeatName($"Familiar{familiarKind}", familiarKind), $"{familiarKind} Familiar.", 
			$"You gain a {familiarKind} familiar.", [FamiliarFeats.TFamiliar], null)
	{
		InnateFeatsCount = innateFeatsCount;
	}
}

public static class Familiar
{
	public static QEffectId QFamiliar = ModManager.RegisterEnumMember<QEffectId>("Familiar");
	public static QEffectId QFamiliarDeployed = ModManager.RegisterEnumMember<QEffectId>("FamiliarDeployed");
	public static QEffect QDeadFamiliar = new ("Dead Familiar",
		"Your familiar has died. It will reappear upon your next long rest.")
	{
		StartOfCombat = async qf => qf.Owner.Overhead("no familiar", Color.Green, qf.Owner + "'s familiar is dead. It will reappear upon your next long rest.")
	};

	public static void Spawn(Creature master)
	{
		var familiar = Create(master);
		familiar.InitiativeControlledBy = master;
		familiar.LongTermEffects = new LongTermEffects();
		familiar.LongTermEffects.BeginningOfCombat(familiar);
		familiar.LongTermEffects.Effects.Clear();
		familiar.Traits.Add(Trait.NoPhysicalUnarmedAttack);
		familiar.AddQEffect(new QEffect
		{
			Id = QDeadFamiliar.Id,
			Source = master,
			WhenMonsterDies = _ => master.AddQEffect(QDeadFamiliar)
		});
		master.Battle.SpawnCreature(familiar, master.OwningFaction, master.Occupies);
	}
	
	public static Creature? GetFamiliar(Creature master)
	{
		return master.Battle.AllCreatures.FirstOrDefault(cr => cr.QEffects.Any(qf => qf.Id == QFamiliar && qf.Source == master));
	}

	public static Illustration GetIllustration(Creature master)
	{
		if (master.PersistentCharacterSheet?.Calculated.Tags.TryGetValue("FamiliarIllustration", out var value ) == true && value is Illustration illustration)
			return illustration;
		return IllustrationName.AnimalForm;
	}

	public static string GetNickname(Creature master)
	{
		if (master.PersistentCharacterSheet?.Calculated.Tags.TryGetValue("FamiliarNickname", out var value) == true && value is string nickname)
			return string.IsNullOrWhiteSpace(nickname) ? nickname : $"{master.Name}'s Familiar";
		
		return $"{master.Name}'s Familiar";
	}
	
	private static Creature Create(Creature master)
	{
		var level = master.Level;
		var perception = 3 + master.Level;
		var hp = level * 5;
		var defenses = new Defenses(
			master.Defenses.GetBaseValue(Defense.AC), 
			master.Defenses.GetBaseValue(Defense.Fortitude), 
			master.Defenses.GetBaseValue(Defense.Reflex), 
			master.Defenses.GetBaseValue(Defense.Will));
		var skills = new Skills(
			acrobatics: 3 + level,
			arcana: level,
			athletics: level,
			crafting: level,
			deception: level,
			diplomacy: level,
			intimidation: level,
			medicine: level,
			nature: level,
			occultism: level,
			performance: level,
			religion: level,
			society: level,
			stealth: 3 + level,
			survival: level,
			thievery: level);
		var speed = 5;
		// RAW familiars don't have their own attribute modifiers, but not assigning them would probably cause a bunch of errors
		var abilities = new Abilities(
			master.Abilities.Strength,
			master.Abilities.Dexterity,
			master.Abilities.Constitution,
			master.Abilities.Intelligence,
			master.Abilities.Wisdom,
			master.Abilities.Charisma);

		var familiar = new Creature(GetIllustration(master), GetNickname(master), [Trait.Animal, Trait.Minion], level, perception, speed, defenses, hp,
			abilities, skills)
			.WithEntersInitiativeOrder(false)
			.AddQEffect(new QEffect {
				StateCheck = sc => {
					if (sc.Owner.HasEffect(QEffectId.Dying) || !sc.Owner.Battle.InitiativeOrder.Contains(sc.Owner))
						return;
					Creature owner = sc.Owner;
					int index = (owner.Battle.InitiativeOrder.IndexOf(owner) + 1) % owner.Battle.InitiativeOrder.Count;
					Creature creature = owner.Battle.InitiativeOrder[index];
					owner.Actions.HasDelayedYieldingTo = creature;
					if (owner.Battle.CreatureControllingInitiative == owner)
						owner.Battle.CreatureControllingInitiative = creature;
					owner.Battle.InitiativeOrder.Remove(sc.Owner);
				}
			});

		familiar.AddQEffect(new QEffect
		{
			Id = QFamiliar,
			Source = master
		});

		return familiar;
	}
}
