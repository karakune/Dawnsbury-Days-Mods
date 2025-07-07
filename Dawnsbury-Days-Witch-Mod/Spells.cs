using System;
using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Mods.Familiars;

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

	public static SpellId PatronsPuppet = ModManager.RegisterNewSpell("PatronsPuppet", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(new ModdedIllustration("AcidicBurstAssets/AcidicBurst.png"), "Patron's Puppet",
					[Trait.Focus, THex, WitchLoader.TWitch],
					"At your unspoken plea, your patron temporarily assumes control over your familiar.",
					"You Command your familiar, allowing it to take its normal actions this turn.",
					Target.Self()
						.WithAdditionalRestriction(master =>
							FamiliarFeats.GetFamiliarCommandRestriction(master, Familiar.GetFamiliar(master),
								isDirectCommand: true))
					, spellLevel, null)
				.WithActionCost(0)
				.WithHexCasting()
				.WithEffectOnSelf(async master =>
				{
					master.AddQEffect(new QEffect
					{
						Traits = [FamiliarFeats.TFamiliarCommand], UsedThisTurn = true,
						ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn
					});

					var familiar = Familiar.GetFamiliar(master);
					if (familiar == null)
						return;

					familiar.Actions.AnimateActionUsedTo(0, ActionDisplayStyle.Slowed);
					familiar.Actions.ActionsLeft = 2;
					await CommonSpellEffects.YourMinionActs(familiar);
				});
		});
	
	public static SpellId PhaseFamiliar = ModManager.RegisterNewSpell("PhaseFamiliar", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(new ModdedIllustration("AcidicBurstAssets/AcidicBurst.png"), "Phase Familiar", [Trait.Focus, THex, Trait.Manipulate, WitchLoader.TWitch],
				"Your patron momentarily recalls your familiar to the ether, shifting it from its solid, physical form into a ghostly version of itself.",
				"Against the triggering damage, your familiar gains resistance 5 to all damage and is immune to precision damage.",
				Target.Self(), spellLevel, null)
				.WithActionCost(Constants.ACTION_COST_REACTION)
				.WithCastsAsAReaction((qfThis, spell, castable) =>
                {
                    // Creature witch = qfThis.Owner;
                    // int reduction = 1;
                    // qfThis.AddGrantingOfTechnical(
                    //     cr => cr.FriendOfAndNotSelf(witch) && cr.DistanceTo(witch) <= 6,
                    //     qfTech =>
                    //     {
                    //         Creature ally = qfTech.Owner;
                    //         qfTech.YouAreDealtDamage = async (qfTech2, attacker, dStuff, defender) =>
                    //         {
                    //             if (!await witch.AskToUseReaction(
                    //                     $"{{b}}Protector's Sacrifice {{icon:Reaction}}{{/b}}\n{ally} is about to take {dStuff.Amount} damage. Redirect {{b}}{reduction}{{/b}} of that damage to yourself?\n{{Red}}Focus Points: {witch.Spellcasting?.FocusPoints ?? 0}{{/Red}}"))
                    //                 return null;
                    //             
                    //             witch.Spellcasting?.UseUpSpellcastingResources(spell);
                    //
                    //             int taken = Math.Min(dStuff.Amount, reduction);
                    //             
                    //             witch.TakeDamage(taken);
                    //             witch.Occupies.Overhead(
                    //                 "-"+taken, Color.Red,
                    //                 $"{witch.Name} redirects {taken} damage to themselves.", "Damage",
                    //                 $"{{b}}{reduction} of {dStuff.Amount}{{/b}} Protector's sacrifice\n{{b}}= {taken}{{/b}}\n\n{{b}}{taken}{{/b}} Total damage", true);
                    //
                    //             return new ReduceDamageModification(reduction, "Protector's sacrifice");
                    //         };
                    //     });
                })
                .WithHeighteningNumerical(spellLevel, 1, inCombat, 1, "The damage you redirect increases by 3.")
				.WithHexCasting();
		});

	public static SpellId ShroudOfNight = ModManager.RegisterNewSpell("ShroudOfNight", 0,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.AshenWind, "Shroud of Night",
					[Trait.Cantrip, Trait.Darkness, THex, Trait.Manipulate, WitchLoader.TWitch],
					"Your patron blankets the target's eyes in darkness.",
					$"{S.FourDegreesOfSuccessReverse(null, "All creatures are concealed to it.", "The target is unaffected.", null)}",
					Target.RangedCreature(6), spellLevel, SpellSavingThrow.Standard(Defense.Will))
				.WithActionCost(1)
				.WithSoundEffect(SfxName.DazzlingFlash)
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					if (result >= CheckResult.Success)
						return;

					// Basically QEffect.Dazzled() but with a different name
					var effect = new QEffect("Shroud of Night", "All creatures are concealed to you (20% miss chance).",
						ExpirationCondition.ExpiresAtEndOfSourcesTurn, source: caster,
						illustration: IllustrationName.Blinded)
					{
						CannotExpireThisTurn = true,
						SubsumedBy = [QEffectId.Blinded],
						SightReductionTo = DetectionStrength.Concealed,
						CountsAsADebuff = true
					};

					target.AddQEffect(effect);
					caster.AddQEffect(QEffect.Sustaining(spell, effect));
				})
				.WithHexCasting();
		});

	public static SpellId Cackle = ModManager.RegisterNewSpell("Cackle", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.HideousLaughter, "Cackle",
					[Trait.Concentrate, Trait.Focus, THex, WitchLoader.TWitch],
					"With a quick burst of laughter, you prolong a magical effect you created.",
					"You Sustain a spell.",
					Target.Self().WithAdditionalRestriction(caster => caster.HasEffect(QEffectId.Sustaining) ? null : "You must be able to sustain a spell."), 
					spellLevel, null)
				.WithActionCost(0)
				.WithHexCasting()
				.WithEffectOnSelf(async caster =>
				{
					if (caster.FindQEffect(QEffectId.Sustaining)?.Tag is QEffect sustainedEffect)
						sustainedEffect.CannotExpireThisTurn = true;
				});
		});
	
	public static SpellId VeilOfDreams = ModManager.RegisterNewSpell("Veil of Dreams", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.VeilOfConfidence, "Veil of Dreams",
					[Trait.Focus, THex, Trait.Manipulate, Trait.Mental, WitchLoader.TWitch],
					"Your patron draws the target into a drowsy state, causing daydreams and sluggishness.",
					$"{S.FourDegreesOfSuccessReverse(null, "As success, and any time the target uses a concentrate action, it must succeed at a DC 5 flat check or the action is disrupted.", "The target takes a –1 status penalty to Perception, attack rolls, and Will saves. This penalty increases to –2 for Will saves against sleep effects.", "The target is unaffected.")}",
					Target.RangedCreature(6), spellLevel, SpellSavingThrow.Standard(Defense.Will))
				.WithActionCost(1)
				.WithSoundEffect(SfxName.AeroBlade)
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					if (result == CheckResult.CriticalSuccess)
						return;
					
					var effect = new QEffect("Drowsy",
						"You have a –1 status penalty to Perception, attack rolls, and Will saves. This penalty increases to –2 for Will saves against sleep effects.",
						ExpirationCondition.ExpiresAtEndOfSourcesTurn, source: caster,
						illustration: IllustrationName.Blinded)
					{
						CannotExpireThisTurn = true,
						CountsAsADebuff = true,
						BonusToDefenses = (effect, action, defense) =>
						{
							Bonus? bonus = null;
							if (defense == Defense.Will)
								bonus = new Bonus(-1, BonusType.Circumstance, "Drowsy");
							if (action != null && action.HasTrait(Trait.Sleep))
								bonus = new Bonus(-2, BonusType.Circumstance, "Drowsy");
							return bonus;
						},
						BonusToAttackRolls = (_, _, _) => new Bonus(-1, BonusType.Circumstance, "Drowsy"),
						BonusToPerception = _ => new Bonus(-1, BonusType.Circumstance, "Drowsy")
					};

					if (result < CheckResult.Success)
					{
						effect.Description += " In addition, Concentrate actions have a 20% chance to be disrupted.";
						effect.FizzleOutgoingActions = async (qf, ca, stringBuilder) =>
						{
							if (!ca.HasTrait(Trait.Concentrate))
								return false;
							
							var flatCheck = Checks.RollFlatCheck(5);
							stringBuilder.AppendLine("Doing a Concentrate action while drowsy: " + flatCheck.Item2);
							return flatCheck.Item1 < CheckResult.Success;
						};
					}

					target.AddQEffect(effect);
					caster.AddQEffect(QEffect.Sustaining(spell, effect));
				})
				.WithHexCasting();
		});

	public static SpellId LifeBoost = ModManager.RegisterNewSpell("Life Boost", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.Heal, "Life Boost",
					[Trait.Focus, Trait.Healing, THex, Trait.Manipulate, Trait.Necromancy, WitchLoader.TWitch],
					"Life force from your patron floods into the target, ensuring they can continue doing your patron's will for just a little longer.",
					"The target gains fast healing 2.",
					Target.RangedFriend(6), spellLevel, null)
				.WithActionCost(1)
				.WithSoundEffect(SfxName.Healing)
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					target.AddQEffect(QEffect.FastHealing(2).WithExpirationAtStartOfSourcesTurn(caster, 4));
				})
				.WithHexCasting();
		});

	public static SpellId SpiritLink = ModManager.RegisterNewSpell("Spirit Link", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.HealingWell, "Spirit Link",
					[Trait.Divine, Trait.Occult, Trait.Healing, Trait.Manipulate],
					"You form a spiritual link with another creature, taking in its pain.",
					"When you Cast this Spell and at the start of each of your turns for the rest of the encounter, if the target is below maximum Hit Points, it regains 2 Hit Points (or the difference between its current and maximum Hit Points, if that's lower). You lose as many Hit Points as the target regained.\nThis is a spiritual transfer, so no effects apply that would increase the Hit Points the target regains or decrease the Hit Points you lose. This transfer also ignores any temporary Hit Points you or the target have. Since this effect doesn't involve vitality or void energy, spirit link works even if you or the target is undead. While the duration persists, you gain no benefit from regeneration or fast healing. You can Dismiss this spell, and if you're ever at 0 Hit Points, spirit link ends automatically.",
					Target.RangedFriend(6), spellLevel, null)
				.WithActionCost(2)
				.WithSoundEffect(SfxName.Healing)
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					var hpToTransfer = 2 * spell.SpellLevel;

					// TODO: prevent Fast Healing from applying
					
					var spiritLinkEffect = new QEffect("Spirit Link",
						$"{target.Name} regains {hpToTransfer} HP every turn if hurt, and you lose as much.",
						ExpirationCondition.Never, caster, IllustrationName.HealingWell)
					{
						Id = QEffectId.RegenerationDeactivated,
						StartOfYourPrimaryTurn = async (effect, creature) =>
						{
							if (creature.HP <= 0)
							{
								effect.ExpiresAt = ExpirationCondition.Immediately;
								return;
							}

							var actualTransferredHp = Math.Min(hpToTransfer, target.MaxHPMinusDrained - target.HP);

							if (actualTransferredHp <= 0)
								return;

							// Heal target
							var currentDamage = target.Damage;
							var damageNewValue = Math.Max(currentDamage - actualTransferredHp, 0);
							var creatureType = target.GetType();

							var prop = creatureType.GetProperty("Damage");

							prop?.SetValue(target, damageNewValue, null);

							// Damage caster
							creature.TakeDamage(actualTransferredHp);
						}
					};

					caster.AddQEffect(spiritLinkEffect);
					caster.AddQEffect(new QEffect
					{
						ProvideMainAction = effect =>
						{
							return new ActionPossibility(new CombatAction(effect.Owner, IllustrationName.HealingWell,
									"Dismiss Spirit Link", [Trait.Concentrate], "", Target.Self())
								.WithActionCost(1)
								.WithEffectOnSelf(_ =>
								{
									spiritLinkEffect.ExpiresAt = ExpirationCondition.Immediately;
									effect.ExpiresAt = ExpirationCondition.Immediately;
								}));
						}
					});
				})
				.WithHeighteningNumerical(spellLevel, 1, true, 1, "The number of Hit Points transferred each time increases by 2.");
		});
	
	public static SpellId BloodWard = RegisterNotImplementedSpell("BloodWard", true);
	public static SpellId ElementalBetrayal = RegisterNotImplementedSpell("ElementalBetrayal", true);
	public static SpellId GustOfWind = RegisterNotImplementedSpell("GustOfWind", false);
	public static SpellId NeedleOfVengeance = RegisterNotImplementedSpell("NeedleOfVengeance", true);
	public static SpellId DeceiverCloak = RegisterNotImplementedSpell("DeceiverCloak", true);
	public static SpellId MadMonkeys = RegisterNotImplementedSpell("MadMonkeys", false);
	public static SpellId MaliciousShadow = RegisterNotImplementedSpell("MaliciousShadow", true);
	public static SpellId PersonalBlizzard = RegisterNotImplementedSpell("PersonalBlizzard", true);
	public static SpellId WallOfWind = RegisterNotImplementedSpell("WallOfWind", false);
	
	private static CombatAction WithHexCasting(this CombatAction combatAction) => combatAction.WithEffectOnSelf(creature =>
	{
		creature.AddQEffect(QHexCasted);
	});

	private static SpellId RegisterNotImplementedSpell(string title, bool isHex)
	{
		return ModManager.RegisterNewSpell(title + " (Not implemented)", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			var spell = Spells.CreateModern(IllustrationName.DawnsburyDaysPureLogo, title,
					[Trait.Concentrate],
					"",
					"",
					Target.Self(),
					spellLevel, null)
				.WithActionCost(0)
				.WithNotImplemented();

			if (isHex)
			{
				spell.Traits.Add(Trait.Focus);
				spell.Traits.Add(THex);
				spell.Traits.Add(WitchLoader.TWitch);
				spell = spell.WithHexCasting();
			}

			return spell;
		});
	}
	
	private static CombatAction WithNotImplemented(this CombatAction combatAction) => combatAction.WithEffectOnSelf(creature =>
	{
		creature.AddQEffect(new QEffect("Not Implemented", "Spell has not been implemented",
			ExpirationCondition.ExpiresAtEndOfYourTurn, null, IllustrationName.DawnsburyDaysPureLogo));
	});
}