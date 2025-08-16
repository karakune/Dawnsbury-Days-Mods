using System;
using System.Linq;
using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
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
				.WithEffectOnEachTarget(async (_, master, _, _) =>
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
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
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
					$"The target gains fast healing {S.HeightenedVariable(2 * spellLevel, 2)}.",
					Target.RangedFriend(6), spellLevel, null)
				.WithActionCost(1)
				.WithSoundEffect(SfxName.Healing)
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					target.AddQEffect(QEffect.FastHealing(2 * spellLevel).WithExpirationAtStartOfSourcesTurn(caster, 4));
				})
				.WithHeighteningNumerical(spellLevel, 1, inCombat, 1, "The fast healing increases by 2.")
				.WithHexCasting();
		});

	public static SpellId SpiritLink = ModManager.RegisterNewSpell("Spirit Link", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.HealingWell, "Spirit Link",
					[Trait.Divine, Trait.Occult, Trait.Healing, Trait.Manipulate],
					"You form a spiritual link with another creature, taking in its pain.",
					$"When you Cast this Spell and at the start of each of your turns for the rest of the encounter, if the target is below maximum Hit Points, it regains {S.HeightenedVariable(2 * spellLevel, 2)} Hit Points (or the difference between its current and maximum Hit Points, if that's lower). You lose as many Hit Points as the target regained.\nThis is a spiritual transfer, so no effects apply that would increase the Hit Points the target regains or decrease the Hit Points you lose. This transfer also ignores any temporary Hit Points you or the target have.\nWhile the duration persists, you gain no benefit from regeneration or fast healing.",
					Target.RangedFriend(6).WithAdditionalConditionOnTargetCreature((self, ally) => self == ally ? Usability.NotUsableOnThisCreature("Cannot target yourself") : Usability.Usable), spellLevel, null)
				.WithActionCost(2)
				.WithSoundEffect(SfxName.Healing)
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					var hpToTransfer = 2 * spell.SpellLevel;
					
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
					
					var fastHealingBlocker = new QEffect("Fast Healing Blocker",
						$"You cannot regain hp from Fast Healing.",
						ExpirationCondition.Never, caster, IllustrationName.HealingWell)
					{
						WhenYouAcquireThis = _ =>
						{
							var fastHealingEffect = caster.FindQEffect(QEffectId.FastHealing);
							if (fastHealingEffect == null)
								return;
							
							var healing = fastHealingEffect.StartOfYourPrimaryTurn;
							fastHealingEffect.StartOfYourPrimaryTurn = async (effect, creature) =>
							{
								if (caster.HasEffect(spiritLinkEffect))
									return;
								healing?.Invoke(effect, creature);
							};
						},
						YouAcquireQEffect = (output, original) =>
						{
							if (original.Id != QEffectId.FastHealing)
								return original;
							
							var healing = original.StartOfYourPrimaryTurn;
							original.StartOfYourPrimaryTurn = async (effect, creature) =>
							{
								if (caster.HasEffect(spiritLinkEffect))
									return;
								healing?.Invoke(effect, creature);
							};
							return original;
						}
					};

					caster.AddQEffect(spiritLinkEffect);
					caster.AddQEffect(fastHealingBlocker);
					caster.AddQEffect(new QEffect
					{
						ProvideMainAction = effect =>
						{
							return new ActionPossibility(new CombatAction(effect.Owner, IllustrationName.HealingWell,
									"Dismiss Spirit Link", [Trait.Concentrate], "", Target.Self())
								.WithActionCost(1)
								.WithEffectOnEachTarget(async (_, _, _, _) =>
								{
									spiritLinkEffect.ExpiresAt = ExpirationCondition.Immediately;
									fastHealingBlocker.ExpiresAt = ExpirationCondition.Immediately;
									effect.ExpiresAt = ExpirationCondition.Immediately;
								}));
						}
					});
				})
				.WithHeighteningNumerical(spellLevel, 1, true, 1, "The number of Hit Points transferred each time increases by 2.");
		});
	
	public static SpellId NeedleOfVengeance = ModManager.RegisterNewSpell("Needle of Vengeance", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.MagneticPinions, "Needle of Vengeance",
					[Trait.Focus, THex, Trait.Manipulate, Trait.Mental, WitchLoader.TWitch],
					"A long, jagged needle jabs into the target foe's psyche whenever it tries to attack a creature your patron holds in special regard.",
					$"Choose yourself or one of your allies. The target takes {S.HeightenedVariable(2*spellLevel, 2)} mental damage any time it uses a hostile action against the named creature, with a basic Will save.",
					Target.MultipleCreatureTargets(Target.RangedFriend(6), Target.Ranged(6)).WithMinimumTargets(2), spellLevel, null)
				.WithActionCost(1)
				.WithSoundEffect(SfxName.Healing)
				.WithEffectOnChosenTargets(async (spell, caster, targets) => 
				{
					Creature chosenAlly;
					Creature chosenVictim;
					
					var chosenCreatures = targets.ChosenCreatures;
					if (chosenCreatures[0].OwningFaction.AlliedFactionOf(caster.OwningFaction))
					{
						chosenAlly = chosenCreatures[0];
						chosenVictim = chosenCreatures[1];
					}
					else
					{
						chosenAlly = chosenCreatures[1];
						chosenVictim = chosenCreatures[0];
					}
					
					var damage = 2 * spell.SpellLevel;
					var effect = new QEffect("Needle of Vengeance", $"You take {damage} damage (basic Will save) every time you take a hostile action towards {chosenAlly.Name}",
							ExpirationCondition.ExpiresAtEndOfSourcesTurn, source: caster,
							illustration: IllustrationName.MagneticPinions)
					{
						CannotExpireThisTurn = true,
						CountsAsADebuff = true,
						AfterYouTakeHostileAction = (qEffect, action) =>
						{
							if (!action.ChosenTargets.GetAllTargetCreatures().Contains(chosenAlly))
								return;
							
							var willSaveResult = CommonSpellEffects.RollSavingThrow(qEffect.Owner, spell, Defense.Will, caster.ClassOrSpellDC());
							
							CommonSpellEffects.DealBasicDamage(spell, caster, qEffect.Owner, willSaveResult, $"{damage}", DamageKind.Mental);
						}
					};
					chosenVictim.AddQEffect(effect);
					
					caster.AddQEffect(QEffect.Sustaining(spell, effect));
				})
				.WithHeighteningNumerical(spellLevel, 1, inCombat, 1, $"The damage increases by 2.")
				.WithHexCasting();
		});
	
	public static SpellId ElementalBetrayal = ModManager.RegisterNewSpell("Elemental Betrayal", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.ElementalBlast, "Elemental Betrayal",
					[Trait.Focus, THex, Trait.Manipulate, Trait.Mental, WitchLoader.TWitch],
					"Your patron uses its superior command of the elements, empowering them to undermine your foe.",
					$"Choose air, earth, metal, fire, water, or wood. The target gains weakness {S.HeightenedVariable((spellLevel + 1) / 2, 2)} to that trait.",
					Target.Ranged(6), spellLevel, null)
				.WithActionCost(1)
				.WithSoundEffect(SfxName.Healing)
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					var prompt = await caster.AskForChoiceAmongButtons(IllustrationName.ElementalBlast,
						$"Which element should {target.Name} be weak to?",
						["air", "earth", "metal", "fire", "water", "wood"]);

					var (element, illustration) = prompt.Index switch
					{
						0 => (Trait.Air, IllustrationName.ElementAir),
						1 => (Trait.Earth, IllustrationName.ElementEarth),
						2 => (Trait.Metal, IllustrationName.ElementMetal),
						3 => (Trait.Fire, IllustrationName.ElementFire),
						4 => (Trait.Water, IllustrationName.ElementWater),
						5 => (Trait.Wood, IllustrationName.ElementWood),
						_ => (Trait.Uncommon, IllustrationName.QuestionMark)
					};

					var effect = new QEffect("Elemental Betrayal",
						$"You have weakness {(spellLevel + 1) / 2} to {element}.",
						ExpirationCondition.ExpiresAtEndOfSourcesTurn, source: caster,
						illustration)
					{
						CannotExpireThisTurn = true,
						CountsAsADebuff = true,
						StateCheck = qEffect => 
						{
							target.WeaknessAndResistance.Weaknesses.Add(new SpecialResistance(element.ToString(), (action, kind) => action?.HasTrait(element) ?? false, (spellLevel + 1) / 2, null));
						}
					};
					
					target.AddQEffect(effect);
					caster.AddQEffect(QEffect.Sustaining(spell, effect));
				})
				.WithHeighteningNumerical(spellLevel, 1, inCombat, 2, $"Increase the weakness by 1.")
				.WithHexCasting();
		});
	
	public static SpellId BloodWard = RegisterNotImplementedSpell("BloodWard", true);
	public static SpellId GustOfWind = RegisterNotImplementedSpell("GustOfWind", false);
	public static SpellId DeceiverCloak = RegisterNotImplementedSpell("DeceiverCloak", true);
	public static SpellId MadMonkeys = RegisterNotImplementedSpell("MadMonkeys", false);
	public static SpellId MaliciousShadow = RegisterNotImplementedSpell("MaliciousShadow", true);
	public static SpellId PersonalBlizzard = RegisterNotImplementedSpell("PersonalBlizzard", true);
	public static SpellId WallOfWind = RegisterNotImplementedSpell("WallOfWind", false);
	
	private static CombatAction WithHexCasting(this CombatAction combatAction) => combatAction.WithEffectOnEachTarget( 
		async (spell, caster, target, result) =>
	{
		if (!caster.HasEffect(QHexCasted))
			caster.AddQEffect(QHexCasted);
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
	
	private static CombatAction WithNotImplemented(this CombatAction combatAction) => combatAction.WithEffectOnEachTarget(async (spell, caster, creature, result) =>
	{
		creature.AddQEffect(new QEffect("Not Implemented", "Spell has not been implemented",
			ExpirationCondition.ExpiresAtEndOfYourTurn, null, IllustrationName.DawnsburyDaysPureLogo));
	});
}