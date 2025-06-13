using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Witch;

public static class WitchSpells
{
	public static Trait THex = ModManager.RegisterTrait("Hex");

	public static QEffect QHexCasted = new ("Hex casted",
		"You casted a hex this round and must wait for the next round to cast another one.",
		ExpirationCondition.ExpiresAtStartOfYourTurn, null)
	{
		PreventTakingAction = ca => ca.HasTrait(THex) ? "You already casted a Hex this round." : null,
	}; 
	
	public static CombatAction WithHexCasting(this CombatAction combatAction) => combatAction.WithEffectOnSelf(creature =>
	{
		creature.AddQEffect(QHexCasted);
	});

	public static SpellId PatronsPuppet = ModManager.RegisterNewSpell("PatronsPuppet", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(new ModdedIllustration("AcidicBurstAssets/AcidicBurst.png"), "Patron's Puppet", [Trait.Focus, THex, WitchLoader.TWitch],
					"At your unspoken plea, your patron temporarily assumes control over your familiar.",
					"You Command your familiar, allowing it to take its normal actions this turn.",
					Target.Self(), spellLevel, null)
				.WithHexCasting();
		});
	public static SpellId PhaseFamiliar = ModManager.RegisterNewSpell("PhaseFamiliar", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(new ModdedIllustration("AcidicBurstAssets/AcidicBurst.png"), "Phase Familiar", [Trait.Focus, THex, Trait.Manipulate, WitchLoader.TWitch],
				"Your patron momentarily recalls your familiar to the ether, shifting it from its solid, physical form into a ghostly version of itself.",
				"Against the triggering damage, your familiar gains resistance 5 to all damage and is immune to precision damage.",
				Target.Self(), spellLevel, null)
				.WithHexCasting();
		});
	
	public static SpellId ShroudOfNight = ModManager.RegisterNewSpell("ShroudOfNight", 0, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
	{
		return Spells.CreateModern(new ModdedIllustration("AcidicBurstAssets/AcidicBurst.png"), "Shroud of Night", [Trait.Cantrip, Trait.Darkness, THex, Trait.Manipulate, WitchLoader.TWitch],
				"Your patron blankets the target's eyes in darkness.",
				"Success The target is unaffected. Failure All creatures are concealed to it.",
				Target.RangedCreature(6), spellLevel, SpellSavingThrow.Standard(Defense.Will))
			.WithActionCost(1)
			// .WithSoundEffect(ModManager.RegisterNewSoundEffect("AcidicBurstAssets/AcidicBurstSfx.mp3"))
			.WithEffectOnEachTarget(async (spell, caster, target, result) =>
			{
				if (result >= CheckResult.Success)
					return;
					
				// target.AddQEffect(new QEffect("Murky Darkness", "All creatures are concealed to you", ExpirationCondition.ExpiresAtEndOfSourcesTurn))
					
				// await CommonSpellEffects.DealBasicDamage(spell, caster, target, result, (spellLevel * 2) + "d6", DamageKind.Acid);
				// await CommonConditionEffects.RollAgainstConfusion()
			})
			.WithHexCasting();
	});
}