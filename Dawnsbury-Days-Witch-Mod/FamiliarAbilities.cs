using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Modding;
using Dawnsbury.Mods.Familiars;

namespace Dawnsbury.Mods.Classes.Witch;

public static class FamiliarAbilities
{
	public static FeatName FNStalkingNight = ModManager.RegisterFeatName("MasStalkingNight", "Familiar of Stalking Night");
	public static FeatName FNRestoredSpirit = ModManager.RegisterFeatName("MasRestoredSpirit", "Familiar of Restored Spirit");
	public static FeatName FNFreezingRime = ModManager.RegisterFeatName("MasFreezingRime", "Familiar of Freezing Rime");
	public static FeatName FNBalancedLuck = ModManager.RegisterFeatName("MasBalancedLuck", "Familiar of Balanced Luck");
	public static FeatName FNFlowingScript = ModManager.RegisterFeatName("MasFlowingScript", "Familiar of Flowing Script");
	public static FeatName FNOngoingMisery = ModManager.RegisterFeatName("MasOngoingMisery", "Familiar of Ongoing Misery");
	public static FeatName FNKeenSenses = ModManager.RegisterFeatName("MasKeenSenses", "Familiar of Keen Senses");
	public static IEnumerable<Feat> CreateFeats()
	{
		var stalkingNightDisplay = MasterAbilities.CreateFamiliarDisplay("MasStalkingNight", "Familiar of Stalking Night");
		yield return stalkingNightDisplay;
		yield return new Feat(FNStalkingNight, null,
				"When you Cast or Sustain a hex, and your familiar is adjacent to an enemy to which it's concealed, hidden, or undetected, the enemy becomes frightened 1.", 
				[], null)
			.WithOnCreature((sheet, master) =>
			{
				var familiar = Familiar.GetFamiliar(master);
				master.AddQEffect(new QEffect
				{
					AfterYouExpendSpellcastingResources = (effect, action) =>
					{
						if (!IsCastingOrSustainingHex(action))
							return;
						
						if (master.QEffects.Contains(Familiar.QDeadFamiliar))
							return;
						
						// Source is master if familiar is not deployed
						var source = familiar ?? master;
						var enemies =
							source.Battle.AllCreatures.Where(c => c.OwningFaction.EnemyFactionOf(source.OwningFaction));
						foreach (var enemy in enemies)
						{
							if (!source.IsAdjacentTo(enemy))
								continue;
							
							var detectionStrength = HiddenRules.DetermineHidden(enemy, source);
							if (detectionStrength == 0)
								continue;

							enemy.AddQEffect(QEffect.Frightened(1));
						}
					}
				});
			})
			.WithOnCreature(owner => owner.AddQEffect(MasterAbilities.GetDisplayQEffect(stalkingNightDisplay.FeatName)));
		
		var restoredSpiritDisplay = MasterAbilities.CreateFamiliarDisplay("MasRestoredSpirit", "Familiar of Restored Spirit");
		yield return restoredSpiritDisplay;
		yield return new Feat(FNRestoredSpirit, null,
				"When you Cast or Sustain a hex, one willing creature within 15 feet of your familiar gains temporary Hit Points equal to 2 + half your level, which last until the start of your next turn.", 
				[], null)
			.WithOnCreature((sheet, master) =>
			{
				var familiar = Familiar.GetFamiliar(master);
				master.AddQEffect(new QEffect
				{
					AfterYouTakeAction = async (effect, action) =>
					{
						if (!IsCastingOrSustainingHex(action))
							return;
						
						if (master.QEffects.Contains(Familiar.QDeadFamiliar))
							return;
						
						// Source is master if familiar is not deployed
						var source = familiar ?? master;

						var target = await source.Battle.AskToChooseACreature(source,
							source.Battle.AllCreatures.Where(c =>
								c.OwningFaction.AlliedFactionOf(source.OwningFaction) && source.DistanceTo(c) <= 3), 
							IllustrationName.Heal, "Choose an ally to receive temp hp",
							"Choose this one", "Pass");

						if (target == null)
							return;
						
						var tempHp = 2 + (master.Level / 2);
						target.GainTemporaryHP(tempHp);
					}
				});
			})
			.WithOnCreature(owner => owner.AddQEffect(MasterAbilities.GetDisplayQEffect(restoredSpiritDisplay.FeatName)));
	}

	private static bool IsCastingOrSustainingHex(CombatAction action)
	{
		return action.HasTrait(WitchSpells.THex) ||
		       action.HasTrait(Trait.SustainASpell) &&
		       action.ReferencedQEffect?.ReferencedSpell != null &&
		       action.ReferencedQEffect.ReferencedSpell.HasTrait(WitchSpells.THex);
	}
}