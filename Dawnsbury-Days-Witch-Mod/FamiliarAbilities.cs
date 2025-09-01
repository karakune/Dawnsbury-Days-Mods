using System;
using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Zoning;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Dawnsbury.Mods.Familiars;

namespace Dawnsbury.Mods.Classes.Witch;

public static class FamiliarAbilities
{
	public static FeatName FNStalkingNight = ModManager.RegisterFeatName("WiFamStalkingNight", "Familiar of Stalking Night");
	public static FeatName FNRestoredSpirit = ModManager.RegisterFeatName("WiFamRestoredSpirit", "Familiar of Restored Spirit");
	public static FeatName FNFreezingRime = ModManager.RegisterFeatName("WiFamFreezingRime", "Familiar of Freezing Rime");
	public static FeatName FNBalancedLuck = ModManager.RegisterFeatName("WiFamBalancedLuck", "Familiar of Balanced Luck");
	public static FeatName FNFlowingScript = ModManager.RegisterFeatName("WiFamFlowingScript", "Familiar of Flowing Script");
	public static FeatName FNOngoingMisery = ModManager.RegisterFeatName("WiFamOngoingMisery", "Familiar of Ongoing Misery");
	public static FeatName FNKeenSenses = ModManager.RegisterFeatName("WiFamKeenSenses", "Familiar of Keen Senses");
	public static IEnumerable<Feat> CreateFeats()
	{
		var stalkingNightDisplay = MasterAbilities.CreateFamiliarDisplay("WiFamStalkingNight", "Familiar of Stalking Night");
		yield return stalkingNightDisplay;
		yield return new Feat(FNStalkingNight, null,
				"When you Cast or Sustain a hex, and your familiar is adjacent to an enemy to which it's concealed, hidden, or undetected, the enemy becomes frightened 1.", 
				[], null)
			.WithOnCreature((sheet, master) =>
			{
				master.AddQEffect(new QEffect
				{
					AfterYouExpendSpellcastingResources = (effect, action) =>
					{
						if (!IsCastingOrSustainingHex(action))
							return;
						
						if (master.QEffects.Contains(Familiar.QDeadFamiliar))
							return;
						
						// Source is master if familiar is not deployed
						var familiar = Familiar.GetFamiliar(master);
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
		
		var restoredSpiritDisplay = MasterAbilities.CreateFamiliarDisplay("WiFamRestoredSpirit", "Familiar of Restored Spirit");
		yield return restoredSpiritDisplay;
		yield return new Feat(FNRestoredSpirit, null,
				"When you Cast or Sustain a hex, one willing creature within 15 feet of your familiar gains temporary Hit Points equal to 2 + half your level, which last until the start of your next turn.", 
				[], null)
			.WithOnCreature((sheet, master) =>
			{
				master.AddQEffect(new QEffect
				{
					AfterYouTakeAction = async (effect, action) =>
					{
						if (!IsCastingOrSustainingHex(action))
							return;
						
						if (master.QEffects.Contains(Familiar.QDeadFamiliar))
							return;
						
						// Source is master if familiar is not deployed
						var familiar = Familiar.GetFamiliar(master);
						var source = familiar ?? master;

						var target = await source.Battle.AskToChooseACreature(source,
							source.Battle.AllCreatures.Where(c =>
								c.OwningFaction.AlliedFactionOf(source.OwningFaction) && source.DistanceTo(c) <= 3), 
							IllustrationName.Heal, "(Familiar of Restored Spirit) Choose an ally to receive temporary Hit Points.",
							"Choose this one", "Pass");

						if (target == null)
							return;
						
						var tempHp = 2 + (master.Level / 2);
						target.GainTemporaryHP(tempHp);
					}
				});
			})
			.WithOnCreature(owner => owner.AddQEffect(MasterAbilities.GetDisplayQEffect(restoredSpiritDisplay.FeatName)));
		
		var balancedLuckDisplay = MasterAbilities.CreateFamiliarDisplay("WiFamBalancedLuck", "Familiar of Balanced Luck");
		yield return balancedLuckDisplay;
		yield return new Feat(FNBalancedLuck, null,
				"When you Cast or Sustain a hex, one creature within 15 feet of your familiar gets either a +1 status bonus (if allied) or a –1 status penalty (if enemy) to its AC until the start of your next turn.", 
				[], null)
			.WithOnCreature((sheet, master) =>
			{
				master.AddQEffect(new QEffect
				{
					AfterYouTakeAction = async (effect, action) =>
					{
						if (!IsCastingOrSustainingHex(action))
							return;
						
						if (master.QEffects.Contains(Familiar.QDeadFamiliar))
							return;
						
						// Source is master if familiar is not deployed
						var familiar = Familiar.GetFamiliar(master);
						var source = familiar ?? master;

						var target = await source.Battle.AskToChooseACreature(source,
							source.Battle.AllCreatures.Where(c => source.DistanceTo(c) <= 3), 
							IllustrationName.ChromaticArmor, "(Familiar of Balanced Luck) Choose a creature to have its AC affected.",
							"Choose this one", "Pass");
						
						if (target == null)
							return;

						var modifier = target.OwningFaction.AlliedFactionOf(source.OwningFaction) ? 1 : -1;

						target.AddQEffect(new QEffect("Balanced Luck", $"You have a {modifier:+0;-#} to AC", 
							ExpirationCondition.ExpiresAtStartOfSourcesTurn, source, IllustrationName.ChromaticArmor)
						{
							BonusToDefenses = (_, _, defense) => defense == Defense.AC
								? new Bonus(modifier, BonusType.Circumstance, "Balanced Luck")
								: null
						});
					}
				});
			})
			.WithOnCreature(owner => owner.AddQEffect(MasterAbilities.GetDisplayQEffect(balancedLuckDisplay.FeatName)));
		
		var freezingRimeDisplay = MasterAbilities.CreateFamiliarDisplay("WiFamFreezingRime", "Familiar of Balanced Luck");
		yield return freezingRimeDisplay;
		yield return new Feat(FNFreezingRime, null,
				"When you Cast or Sustain a hex, you can cause ice to form in a 5-foot burst centered on a square of your familiar's space. Those squares are difficult terrain until the start of your next turn.", 
				[], null)
			.WithOnCreature((sheet, master) =>
			{
				master.AddQEffect(new QEffect
				{
					AfterYouTakeAction = async (effect, action) =>
					{
						if (!IsCastingOrSustainingHex(action))
							return;
						
						if (master.QEffects.Contains(Familiar.QDeadFamiliar))
							return;
						
						// Source is master if familiar is not deployed
						var familiar = Familiar.GetFamiliar(master);
						var source = familiar ?? master;

						var response = await source.Battle.AskForConfirmation(source, IllustrationName.WintersClutch,
							"(Familiar of Freezing Rime) Create a 5-foot burst of difficult terrain around your familiar?",
							"Yes");
						
						if (!response)
							return;

						var tiles = source.Battle.Map.AllTiles.Where(tile => source.DistanceTo(tile) <= 1).ToList();

						var burstQf = new QEffect
						{
							Source = source,
							ExpiresAt = ExpirationCondition.ExpiresAtStartOfSourcesTurn
						};

						source.AddQEffect(burstQf);
						
						Zone.SpawnStaticAndApply(burstQf, tiles, zone =>
						{
							zone.TileEffectCreator = (Func<Tile, TileQEffect>) (tile => new TileQEffect(tile)
							{
								Illustration = (Illustration) new[]
								{
									IllustrationName.SnowTile1,
									IllustrationName.SnowTile2,
									IllustrationName.SnowTile3,
									IllustrationName.SnowTile4
								}.GetRandomVisualOnly(),
								TransformsTileIntoDifficultTerrain = true,
							});
						});
					}
				});
			})
			.WithOnCreature(owner => owner.AddQEffect(MasterAbilities.GetDisplayQEffect(freezingRimeDisplay.FeatName)));
	}

	private static bool IsCastingOrSustainingHex(CombatAction action)
	{
		return action.HasTrait(WitchSpells.THex) ||
		       action.HasTrait(Trait.SustainASpell) &&
		       action.ReferencedQEffect?.ReferencedSpell != null &&
		       action.ReferencedQEffect.ReferencedSpell.HasTrait(WitchSpells.THex);
	}
}