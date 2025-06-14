using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dawnsbury.Campaign.LongTerm;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
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

namespace Dawnsbury.Mods.Classes.Witch;

public static class FamiliarFeats
{
	public static FeatName FNFamiliar = ModManager.RegisterFeatName("Familiar", "Familiar");
	public static FeatName FNSnake = ModManager.RegisterFeatName("FamiliarRaven", "Raven");

	public static Trait TFamiliar = ModManager.RegisterTrait("Familiar");
	public static QEffect QDeadFamiliar = new ("Dead Familiar",
		"Your familiar has died. It will reappear upon your next long rest.")
	{
		StartOfCombat = async qf => qf.Owner.Occupies.Overhead("no familiar", Color.Green, qf.Owner + "'s familiar is dead. It will reappear upon your next long rest.")
	};
	
	// public static QEffect QFamiliarCommanded = new ("Familiar was commanded",
	// 	"You commanded your familiar this turn.", ExpirationCondition.ExpiresAtStartOfYourTurn, null)
	// {
	// 	ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn,
	// 	PreventTakingAction = ca => ca.HasTrait(TFamiliar) ? "You commanded your familiar this turn." : null
	// }; 
	
	public static IEnumerable<Feat> CreateFeats()
	{
		yield return CreateFamiliarFeat(FNSnake, IllustrationName.AnimalFormSnake, "Snake Familiar", new List<Feat>());
	}

	private static Feat CreateFamiliarFeat(FeatName featName, Illustration illustration, string flavorText, List<Feat> innateFeats)
	{
		// var familiarStatBlock = Familiar.Create(owner.ToCreature(owner.MaximumLevel), illustration, $"{owner.Name}'s Familiar", innateFeats);
		// familiarStatBlock.RegeneratePossibilities();
		// familiarStatBlock.RecalculateLandSpeedAndInitiative();
		// var rulesText = "Your familiar has the following characteristics at level 1:\n\n" + RulesBlock.CreateCreatureDescription(familiarStatBlock);

		return new Feat(featName, flavorText, "", [TFamiliar], null)
			.WithOnCreature(owner =>
			{
				if (owner.HasEffect(QDeadFamiliar))
					return;
				
				// var familiar = Familiar.Create(master, illustration, $"{master.Name}'s Familiar", innateFeats);
				owner.AddQEffect(new QEffect("Familiar", "You patron granted you a familiar")
				{
					StartOfCombat = async qf =>
					{
						var master = qf.Owner;
						var familiar = Familiar.Create(master, illustration, $"{master.Name}'s Familiar", innateFeats);
						familiar.MainName = $"{master.Name}'s Familiar";
						familiar.InitiativeControlledBy = master;
						familiar.LongTermEffects = new LongTermEffects();
						familiar.LongTermEffects.BeginningOfCombat(familiar);
						familiar.LongTermEffects.Effects.Clear();
						familiar.AddQEffect(new QEffect
						{
							Id = QDeadFamiliar.Id,
							Source = master,
							WhenMonsterDies = _ => master.AddQEffect(QDeadFamiliar)
						});
						master.Battle.SpawnCreature(familiar, master.OwningFaction, master.Occupies);
					}
				});

				owner.AddQEffect(new QEffect("CommandFamiliar", "You command your familiar")
				{
					ProvideMainAction = effect =>
					{
						var master = effect.Owner;
						var familiar = Familiar.GetFamiliar(master);
						if (familiar == null)
							return null;
						
						var combatAction = new CombatAction(master, familiar.Illustration, "Command Familiar", [TFamiliar],
							"Take 2 actions as your animal companion.\n\nYou can only command your animal companion once per turn.",
							Target.Self()
							.WithAdditionalRestriction(_ => GetFamiliarCommandRestriction(effect, familiar)))
							.WithActionCost(1)
							.WithEffectOnSelf(async _ =>
							{
								// master.AddQEffect(QFamiliarCommanded);
								await CommonSpellEffects.YourMinionActs(familiar);
							});
						
						return new ActionPossibility(combatAction);
					}
				});
			});
	}

	private static string? GetFamiliarCommandRestriction(
		QEffect qfMaster,
		Creature familiar)
	{
		if (qfMaster.UsedThisTurn)
			return "You already commanded your familiar this turn.";
		if (familiar.HasEffect(QEffectId.Paralyzed))
			return "Your familiar is paralyzed.";
		return familiar.Actions.ActionsLeft == 0 && (familiar.Actions.QuickenedForActions == null || familiar.Actions.UsedQuickenedAction) ? "Your familiar has no actions it could take." : null;
	}
}

public static class Familiar
{	
	
	public static Creature Create(Creature master, Illustration illustration, string name, List<Feat> innateFeats)
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
		var speed = 25;
		// RAW familiars don't have their own attribute modifiers, but not assigning them would probably cause a bunch of errors
		var abilities = new Abilities(
			master.Abilities.Strength,
			master.Abilities.Dexterity,
			master.Abilities.Constitution,
			master.Abilities.Intelligence,
			master.Abilities.Wisdom,
			master.Abilities.Charisma);

		var familiar = new Creature(illustration, name, new List<Trait>(), level, perception, speed, defenses, hp,
			abilities, skills);

		foreach (var feat in innateFeats)
		{
			familiar = familiar.WithFeat(feat.FeatName);
		}

		familiar.AddQEffect(new QEffect
		{
			Id = QEffectId.RangersCompanion,
			PreventTakingAction = ca => ca.HasTrait(Trait.Strike) ? "Familiars cannot strike" : null,
			Source = master
		});

		return familiar;
	}
	
	public static Creature? GetFamiliar(Creature master)
	{
		return master.Battle.AllCreatures.FirstOrDefault(cr => cr.QEffects.Any(qf => qf.Id == QEffectId.RangersCompanion && qf.Source == master));
	}
}