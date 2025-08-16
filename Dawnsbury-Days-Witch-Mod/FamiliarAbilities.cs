using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Modding;
using Dawnsbury.Mods.Familiars;

namespace Dawnsbury.Mods.Classes.Witch;

public static class FamiliarAbilities
{
	public static FeatName FNStalkingNight = ModManager.RegisterFeatName("MasStalkingNight", "Familiar of Stalking Night");
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
						if (!action.HasTrait(WitchSpells.THex) && 
						    action.HasTrait(Trait.SustainASpell) && 
						    action.ReferencedQEffect is { ReferencedSpell: not null } &&
						    !action.ReferencedQEffect.ReferencedSpell.HasTrait(WitchSpells.THex))
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
	}
}