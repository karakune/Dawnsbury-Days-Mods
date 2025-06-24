using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Familiars;

public static class MasterAbilities
{
	public static IEnumerable<Feat> CreateFeats()
	{
		var cantripConnectionDisplay = CreateFamiliarDisplay("MasCantripConnection", "Cantrip Connection");
		yield return cantripConnectionDisplay;
		yield return new Feat(ModManager.RegisterFeatName("MasCantripConnection", "Cantrip Connection"), null,
					"You can prepare an additional cantrip or, if you have a repertoire, designate a cantrip to add to your repertoire.",
					[FamiliarAbilities.TFamiliarAbilitySelection], null)
				.WithOnSheet(sheet =>
				{
					if (sheet.PreparedSpells.Keys.Count > 0)
						sheet.PreparedSpells[sheet.PreparedSpells.Keys.First()].Slots
							.Add(new FreePreparedSpellSlot(0, "Familiar:Cantrip1"));
					else if (sheet.SpellRepertoires.Keys.Count > 0)
					{
						var repertoireTrait = sheet.SpellRepertoires.Keys.First();
						var repertoireTradition = sheet.SpellTraditionsKnown.First();
						sheet.AddNewSpontaneousSpells("Familiar:Cantrip1", 0, repertoireTrait, repertoireTradition, 0,
							1);
					}
				})
				.WithPrerequisite(sheet => sheet.PreparedSpells.Keys.Count > 0 || sheet.SpellRepertoires.Keys.Count > 0,
					"You must be able to prepare cantrips or add them to your repertoire.")
				.WithOnCreature(owner => owner.AddQEffect(GetDisplayQEffect(cantripConnectionDisplay.FeatName)));
		
		var familiarFocusDisplay = CreateFamiliarDisplay("MasFamiliarFocus", "Familiar Focus");
		yield return familiarFocusDisplay;
		yield return new Feat(ModManager.RegisterFeatName("MasFamiliarFocus", "Familiar Focus"), null,
				"Once per day, your familiar can use 2 actions with the concentrate trait to restore 1 Focus Point to your focus pool, up to your usual maximum.", [FamiliarAbilities.TFamiliarAbilitySelection], null)
			.WithOnCreature((sheet, owner) =>
			{
				owner.AddQEffect(new QEffect
				{
					ProvideMainAction = effect =>
					{
						var master = effect.Owner;
						var familiar = Familiar.GetFamiliar(master);
						if (familiar == null)
							return null;
						
						var combatAction = new CombatAction(master, familiar.Illustration, "Familiar Focus", [FamiliarFeats.TFamiliar],
								"Your familiar uses two actions to restore one of your Focus Points.",
								Target.Self()
									.WithAdditionalRestriction(_ =>
									{
										if (master.PersistentUsedUpResources.UsedUpActions.Contains("FamiliarFocus"))
											return "You can only use this once per day.";
										
										if (familiar.HasEffect(QEffectId.Paralyzed))
											return "Your familiar is paralyzed.";
										
										return familiar.Actions.ActionsLeft >= 2
											? "Your familiar must have two actions it can take."
											: null;
									}))
							.WithActionCost(1)
							.WithEffectOnSelf(async _ =>
							{
								if (master.Spellcasting == null || master.Spellcasting.FocusPoints >=
								    master.Spellcasting.FocusPointsMaximum)
									return;
								
								master.PersistentUsedUpResources.UsedUpActions.Add("FamiliarFocus");
								
								master.Spellcasting.FocusPoints += 1;
								familiar.Actions.ActionsLeft = 0;
							});
						
						return new ActionPossibility(combatAction);
					}
				});
			})
			.WithPrerequisite(sheet => sheet.FocusPointCount > 0, "You must have a focus pool.")
			.WithOnCreature(owner => owner.AddQEffect(GetDisplayQEffect(familiarFocusDisplay.FeatName)));
	}

	private static Feat CreateFamiliarDisplay(string technicalName, string displayName)
	{
		return new Feat(ModManager.RegisterFeatName($"{technicalName}Display", displayName), 
			null, "", [FamiliarAbilities.TFamiliarAbility], null)
			.WithOnCreature(creature => creature.AddQEffect(new QEffect(displayName, "")));
	}

	private static QEffect GetDisplayQEffect(FeatName displayed)
	{
		return new QEffect
		{
			StartOfCombat = async effect =>
			{
				var master = effect.Owner;
				var familiar = Familiar.GetFamiliar(master);
				familiar?.WithFeat(displayed);
			}
		};
	}
}