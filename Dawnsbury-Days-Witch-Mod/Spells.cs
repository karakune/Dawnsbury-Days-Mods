using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Zoning;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Mods.DeployableFamiliars;

namespace Dawnsbury.Mods.Classes.Witch;

public static class WitchSpells
{
	public static Trait THex = ModManager.RegisterTrait("Hex");
	private static QEffectId NudgeFateId = ModManager.RegisterEnumMember<QEffectId>("NudgeFate");
	public static QEffectId DiscernSecretsId = ModManager.RegisterEnumMember<QEffectId>("DiscernSecrets");
	public static QEffectId DiscernSecretsUsedThisTurnId = ModManager.RegisterEnumMember<QEffectId>("DiscernSecretsUsedThisTurn");
	private static QEffectId DiscernSecretsImmunityId = ModManager.RegisterEnumMember<QEffectId>("DiscernSecretsImmunity");

	public static QEffect QHexCasted = new ("Hex casted",
		"You casted a hex this round and must wait for the next round to cast another one.",
		ExpirationCondition.ExpiresAtStartOfYourTurn, null)
	{
		PreventTakingAction = ca => ca.HasTrait(THex) ? "You already casted a Hex this round." : null,
	}; 

	public static SpellId PatronsPuppet = ModManager.RegisterNewSpell("PatronsPuppet", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(
					new ModdedIllustration("AcidicBurstAssets/AcidicBurst.png"),
					"Patron's Puppet",
					[WitchLoader.ModName, Trait.Focus, THex, WitchLoader.TWitch, Trait.Uncommon],
					"At your unspoken plea, your patron temporarily assumes control over your familiar.",
					"You Command your familiar, allowing it to take its normal actions this turn.",
					Target.Self()
						.WithAdditionalRestriction(master =>
							ModData.CommonRequirements.WhyCannotCommand(master)),
					spellLevel,
					null)
				.WithActionCost(0)
				.WithHexCasting()
				.WithEffectOnEachTarget(async (spell, master, _, _) =>
				{	
					Possibilities poss = Possibilities
						.Create(master)
						.Filter(ap =>
						{
							ap.CombatAction.ActionCost = 0;
							if (!DeployableFamiliars.FamiliarAbilities.IsFamiliarAction(ap.CombatAction))
								return false;
							ap.RecalculateUsability();
							return true;
						});
					poss.CannotPass = false;
                            
					poss.Sections.Add(new PossibilitySection("Pass")
					{
						Possibilities = [new ActionPossibility(new CombatAction(
								master,
								IllustrationName.EndTurn,
								"Pass",
								[Trait.Basic, Trait.UsableEvenWhenUnconsciousOrParalyzed, Trait.DoesNotPreventDelay],
								"Do nothing.",
								Target.Self())
							.WithTag("PassCommandingFamiliar")
							.WithActionCost(0))]
					});
        
					Creature? active = master.Battle.ActiveCreature;
					master.Battle.ActiveCreature = master;
					master.Possibilities = poss;
        
					List<Option> actions = await master.Battle.GameLoop.CreateActions(
						master,
						poss,
						null);
					master.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
					await master.Battle.GameLoop.OfferOptions(master, actions, true);
        
					master.Battle.ActiveCreature = active;

					if (master.Actions.ActionHistoryThisTurn.LastOrDefault() is { Tag: "PassCommandingFamiliar" })
						RefundSpell();
					
					return;
					
					void RefundSpell()
					{
						master.Actions.RevertExpendingOfResources(0, spell);
						master.Spellcasting?.RevertExpendingOfResources(spell);
						master.RemoveAllQEffects(qf => qf == QHexCasted);
					}
				});
		});
	
	public static SpellId PhaseFamiliar = ModManager.RegisterNewSpell("PhaseFamiliar", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.Invisibility, "Phase Familiar", [WitchLoader.ModName, Trait.Focus, THex, Trait.Manipulate, WitchLoader.TWitch, Trait.Uncommon],
				"Your patron momentarily recalls your familiar to the ether, shifting it from its solid, physical form into a ghostly version of itself.",
				$"Against the triggering damage, your familiar gains resistance {S.HeightenedVariable(3 + (spellLevel * 2), 5)} to all damage and is immune to precision damage.",
				Target.Uncastable(), spellLevel, null)
				.WithActionCost(Constants.ACTION_COST_REACTION)
				.WithCastsAsAReaction((qfThis, spell, castable) =>
                {
                    var witch = qfThis.Owner;
                    var reduction = 3 + (spellLevel * 2);
                    qfThis.AddGrantingOfTechnical(
                        cr => cr.FriendOfAndNotSelf(witch) && cr.DistanceTo(witch) <= 12,
                        qfTech =>
                        {
	                        var familiar = DeployableFamiliarTag.FindFamiliar(witch);
                            var ally = qfTech.Owner;
                            if (familiar == null || ally != familiar)
	                            return;

                            qfTech.YouAreDealtDamage = async (qfTech2, attacker, damageStuff, defender) =>
                            {
	                            var focusPoints = witch.Spellcasting?.FocusPoints ?? 0;

	                            if (focusPoints <= 0)
		                            return null;
	                            
                                if (!await witch.AskToUseReaction($"{{b}}Phase Familiar {{icon:Reaction}}{{/b}}\n{ally} is about to take {damageStuff.Amount} damage. Grant resistance {{b}}{reduction}{{/b}} to all damage and immunity to precision damage for this attack?\n{{Red}}Focus Points: {focusPoints}{{/Red}}"))
                                    return null;
                                
                                witch.Spellcasting?.UseUpSpellcastingResources(spell);

                                // TODO: immunity fails because YouAreDealtDamage happens after resistance calculation
                                defender.AddQEffect(QEffect.TraitImmunity(Trait.PrecisionDamage).WithExpirationEphemeral());
                                return new ReduceDamageModification(reduction, "Phase Familiar");
                            };
                        });
                })
                .WithHeighteningNumerical(spellLevel, 1, inCombat, 1, "Increase the resistance by 2.")
				.WithHexCasting();
		});

	public static SpellId ShroudOfNight = ModManager.RegisterNewSpell("ShroudOfNight", 0,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.AshenWind, "Shroud of Night",
					[WitchLoader.ModName, Trait.Cantrip, Trait.Darkness, THex, Trait.Manipulate, WitchLoader.TWitch, Trait.Uncommon],
					"Your patron blankets the target's eyes in darkness.",
					$"{S.FourDegreesOfSuccessReverse(null, "All creatures are concealed to it.", "The target is unaffected.", null)[3..]}",
					Target.Ranged(6), spellLevel, SpellSavingThrow.Standard(Defense.Will))
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

	public static SpellId StokeTheHeart = ModManager.RegisterNewSpell("StokeTheHeart", 0,
		(spellId, spellcaster, spellLevel, inCombat, spellInfo) =>
		{
			return Spells.CreateModern(IllustrationName.FlashForge, "Stoke the Heart",
					[WitchLoader.ModName, Trait.Cantrip, Trait.Concentrate, Trait.Emotion, THex, WitchLoader.TWitch, Trait.Uncommon],
					"Your patron fills a creature with fervor, empowering their blows.",
					$"The target gains a +{S.HeightenedVariable(2 + (spellLevel / 2), 2)} status bonus to damage rolls.",
					Target.RangedFriend(6), spellLevel, null)
				.WithActionCost(1)
				.WithSoundEffect(SfxName.ElementalBlastMetal)
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					var effect = new QEffect("Stoke the Heart", $"You have a +{S.HeightenedVariable(2 + (spellLevel / 2), 2)} status bonus to damage rolls.",
						ExpirationCondition.ExpiresAtEndOfSourcesTurn, source: caster,
						illustration: IllustrationName.FlashForge)
					{
						CannotExpireThisTurn = true,
						BonusToDamage = (qEffect, action, arg3) => new Bonus(2 + (spellLevel / 2), BonusType.Status, "Stoke the Heart", true) 
					};

					target.AddQEffect(effect);
					caster.AddQEffect(QEffect.Sustaining(spell, effect));
				})
				.WithHeighteningNumerical(spellLevel, 1, inCombat, 2, $"The status bonus to damage increases by 1.")
				.WithHexCasting();
		});

	public static SpellId NudgeFate =  ModManager.RegisterNewSpell("NudgeFate", 0,
		(spellId, spellcaster, spellLevel, inCombat, spellInfo) =>
		{
			return Spells.CreateModern(IllustrationName.Chaos, "Nudge Fate",
					[WitchLoader.ModName, Trait.Cantrip, Trait.Concentrate, THex, WitchLoader.TWitch, Trait.Uncommon],
					"The barest spin of your patron's spool is enough to alter fate.",
					"When the target fails an attack roll, skill check, or saving throw and a +1 status bonus would turn a critical failure into a failure, or failure into a success, you grant the target a +1 status bonus to the check retroactively, changing the outcome appropriately. The spell then ends.",
					Target.RangedFriend(6), spellLevel, null)
				.WithActionCost(1)
				.WithSoundEffect(SfxName.Fabric)
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					var effect = new QEffect("Nudge Fate", "If you fail (or critically fail) an attack roll, skill check, or saving throw by 1, gain +1",
						ExpirationCondition.Never, source: caster,
						illustration: IllustrationName.Chaos)
					{
						Id = NudgeFateId,
						RerollSavingThrow = async (self, breakdownResult, action) =>
						{
							var breakdown = CombatActionExecution.BreakdownSavingThrowForTooltip(action, self.Owner, action.SavingThrow!);
							var successDc = breakdown.TotalDC;
							var totalRollValue = breakdownResult.TotalRollValue;

							if (successDc - totalRollValue != 1 && successDc - totalRollValue != 11)
								return RerollDirection.DoNothing;
							
							self.Owner.AddQEffect(new QEffect
							{
								ExpiresAt = ExpirationCondition.Ephemeral,
								BonusToAllChecksAndDCs = _ => new Bonus(1, BonusType.Status, "Nudge Fate", true)
							});
							
							self.ExpiresAt = ExpirationCondition.Immediately;

							return RerollDirection.KeepRollButRedoCalculation;
						},
						RerollActiveRoll = async (self, breakdownResult, action, actionTarget) =>
						{
							var breakdown = CombatActionExecution.BreakdownAttackForTooltip(action, actionTarget);
							var successDc = breakdown.TotalDC;
							var totalRollValue = breakdownResult.TotalRollValue;

							if (successDc - totalRollValue != 1 && successDc - totalRollValue != 11)
								return RerollDirection.DoNothing;
							
							self.Owner.AddQEffect(new QEffect
							{
								ExpiresAt = ExpirationCondition.Ephemeral,
								BonusToAllChecksAndDCs = _ => new Bonus(1, BonusType.Status, "Nudge Fate", true)
							});
							
							self.ExpiresAt = ExpirationCondition.Immediately;

							return RerollDirection.KeepRollButRedoCalculation;
						}
					};

					foreach (var creature in target.Battle.AllCreatures)
					{
						var instance = creature.FindQEffect(NudgeFateId);
						if (instance != null && instance.Source == caster)
							instance.ExpiresAt = ExpirationCondition.Immediately;
					}

					target.AddQEffect(effect);
				})
				.WithHexCasting();
		});

	public static SpellId ClingingIce = ModManager.RegisterNewSpell("ClingingIce", 0,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.WintersClutch, "Clinging Ice",
					[WitchLoader.ModName, Trait.Cantrip, Trait.Cold, THex, Trait.Manipulate, WitchLoader.TWitch, Trait.Uncommon],
					$"Freezing sleet and heavy snowfall collect on the target's feet and legs.",
					$"Deal {S.HeightenedVariable(spellLevel, 1)}d4 cold damage and other effects depending on the target's Reflex save.{S.FourDegreesOfSuccessReverse("The target takes double damage and a –10-foot circumstance penalty to its Speeds until the spell ends.", "The target takes full damage and a –5-foot circumstance penalty to its Speeds until the spell ends.", "The target takes half damage.", "The target is unaffected.")}",
					Target.Ranged(6), spellLevel, SpellSavingThrow.Standard(Defense.Reflex))
				.WithActionCost(1)
				.WithSoundEffect(SfxName.WintersClutch)
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					var speedReduction = 0;
					var damage = new DamageEvent(spell, target, result,
					[
						new KindedDamage(DiceFormula.FromText($"{spellLevel}d4"),
							DamageKind.Cold)
					], result == CheckResult.CriticalFailure, result == CheckResult.Success);
					switch (result)
					{
						case CheckResult.CriticalFailure:
							speedReduction = 2;
							await CommonSpellEffects.DealDirectDamage(damage);
							break;
						case CheckResult.Failure:
							speedReduction = 1;
							await CommonSpellEffects.DealDirectDamage(damage);
							break;
						case CheckResult.Success:
							await CommonSpellEffects.DealDirectDamage(damage);
							break;
						case CheckResult.CriticalSuccess:
							break;
						default:
							throw new ArgumentOutOfRangeException(nameof(result), result, null);
					}
					
					if (result >= CheckResult.Success)
						return;

					var effect = new QEffect("Clinging Ice", $"You have a -{speedReduction * 5}-foot speed reduction.",
						ExpirationCondition.ExpiresAtEndOfSourcesTurn, source: caster,
						illustration: IllustrationName.WintersClutch)
					{
						CannotExpireThisTurn = true,
						CountsAsADebuff = true,
						BonusToAllSpeeds = _ => new Bonus(-speedReduction, BonusType.Circumstance, caster.Name, false) 
					};

					target.AddQEffect(effect);
					caster.AddQEffect(QEffect.Sustaining(spell, effect));
				})
				.WithHeighteningNumerical(spellLevel, 1, inCombat, 1, "The damage increases by 1d4.")
				.WithHexCasting();
		});

	public static SpellId DiscernSecrets = ModManager.RegisterNewSpell("DiscernSecrets", 0,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.EyeOfFortune, "Discern Secrets",
					[Trait.Cantrip, THex, Trait.Manipulate, WitchLoader.TWitch, Trait.Uncommon],
					$"Your patron deigns to whisper a few secrets.",
					$"As long as you sustain the spell, the target can Seek or Recall Weakness once per turn as a {{icon:FreeAction}} free action, with a +1 status bonus. Once the spell ends, the target is immune to Discern Secrets for the rest of the encounter.",
					new DiscernSecretsTarget(), spellLevel, null)
				.WithActionCost(1)
				.WithSoundEffect(SfxName.BookOpen)
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					var effect = new QEffect(
						"Discern Secrets",
						"Your next Seek or Recall Weakness is a free action, with a +1 status bonus.",
						ExpirationCondition.ExpiresAtEndOfSourcesTurn,
						caster,
						IllustrationName.EyeOfFortune)
					{
						Id = DiscernSecretsId,
						CannotExpireThisTurn = true,
						BonusToSkillChecks = (_, action, _) =>
							action.ActionId == ActionId.Seek || action.Name.Contains("Recall Weakness")
								? new Bonus(1, BonusType.Status, "Discern Secrets")
								: null,
						AfterYouTakeAction = async (effect, action) =>
						{
							if (action.ActionId == ActionId.Seek
							    || action.Name.Contains("Recall Weakness"))
							{
								effect.Owner.AddQEffect(new QEffect()
								{
									Id = DiscernSecretsUsedThisTurnId,
									ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn
								});
							}
						},
						WhenYouAcquireThis = _ =>
						{
							target.AddQEffect(new QEffect()
								{ Id = DiscernSecretsImmunityId, ExpiresAt = ExpirationCondition.Never });
						}
					};
					
					target.AddQEffect(effect);
					caster.AddQEffect(QEffect.Sustaining(spell, effect));
				})
				.WithHeightenedAtSpecificLevel(spellLevel, 5, inCombat, "You can target two creatures instead of one.")
				.WithHexCasting();
		});
	
	public class DiscernSecretsTarget : GeneratorTarget
	{
		public override GeneratedTargetInSequence? GenerateNextTarget()
		{
			int count = OwnerAction.ChosenTargets.ChosenCreatures.Count;
			int maxTargets = OwnerAction.SpellLevel >= 5 ? 2 : 1;
			if (count == maxTargets)
			{
				return null;
			}
			return new GeneratedTargetInSequence(RangedFriend(6)
					.WithAdditionalConditionOnTargetCreature((_, target) => target.HasEffect(DiscernSecretsImmunityId) ? Usability.NotUsableOnThisCreature("Immune") : Usability.Usable),
				$" ({count + 1}/{maxTargets})")
			{
				DisableConfirmNoMoreTargets = (count == 0),
			};
		}
	}


	public static SpellId Cackle = ModManager.RegisterNewSpell("Cackle", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.HideousLaughter, "Cackle",
					[WitchLoader.ModName, Trait.Concentrate, Trait.Focus, THex, WitchLoader.TWitch, Trait.Uncommon],
					"With a quick burst of laughter, you prolong a magical effect you created.",
					"You Sustain a spell.",
					Target.Self().WithAdditionalRestriction(caster => caster.HasEffect(QEffectId.Sustaining) ? null : "You must be able to sustain a spell."), 
					spellLevel, null)
				.WithActionCost(0)
				.WithHexCasting()
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					List<QEffect> sustainableEffects = caster.QEffects.Where(effect => effect is 
					{ 
						Id: QEffectId.Sustaining, 
						Tag: QEffect
						{
							CannotExpireThisTurn: false
						}
					}).Select(e => e.Tag as QEffect).ToList()!;

					QEffect effectToSustain;
					switch (sustainableEffects.Count)
					{
						case 0:
							caster.Actions.RevertExpendingOfResources(0, spell);
							caster.Spellcasting?.RevertExpendingOfResources(spell);
							caster.RemoveAllQEffects(qf => qf == QHexCasted);
							return;
						case 1:
							effectToSustain = sustainableEffects[0];
							break;
						default:
							var optionNames = sustainableEffects.Select(effect => $"{effect.Name} (target: {effect.Owner.Name})");
							
							var choiceResult = await caster.AskForChoiceAmongButtons(IllustrationName.HideousLaughter,
								"Which spell should be sustained?", [.. optionNames]);
							
							effectToSustain = sustainableEffects[choiceResult.Index];
							break;
					}

					effectToSustain.CannotExpireThisTurn = true;
				});
		});
	
	public static SpellId VeilOfDreams = ModManager.RegisterNewSpell("Veil of Dreams", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.VeilOfConfidence, "Veil of Dreams",
					[WitchLoader.ModName, Trait.Focus, THex, Trait.Manipulate, Trait.Mental, WitchLoader.TWitch, Trait.Uncommon],
					"Your patron draws the target into a drowsy state, causing daydreams and sluggishness.",
					$"{S.FourDegreesOfSuccessReverse(null, "As success, and any time the target uses a concentrate action, it must succeed at a DC 5 flat check or the action is disrupted.", "The target takes a –1 status penalty to Perception, attack rolls, and Will saves. This penalty increases to –2 for Will saves against sleep effects.", "The target is unaffected.")}",
					Target.Ranged(6), spellLevel, SpellSavingThrow.Standard(Defense.Will))
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
					[WitchLoader.ModName, Trait.Focus, Trait.Healing, THex, Trait.Manipulate, Trait.Necromancy, WitchLoader.TWitch, Trait.Uncommon],
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
					[WitchLoader.ModName, Trait.Divine, Trait.Occult, Trait.Healing, Trait.Manipulate],
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

							var unconsciousEffect = target.FindQEffect(QEffectId.Unconscious);
							if (unconsciousEffect != null) 
								unconsciousEffect.ExpiresAt = ExpirationCondition.Immediately;
							
							var dyingEffect = target.FindQEffect(QEffectId.Dying);
							if (dyingEffect != null) 
								dyingEffect.ExpiresAt = ExpirationCondition.Immediately;

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
					[WitchLoader.ModName, Trait.Focus, THex, Trait.Manipulate, Trait.Mental, WitchLoader.TWitch, Trait.Uncommon],
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
						AfterYouTakeHostileAction = async (qEffect, action) =>
						{
							if (!action.ChosenTargets.GetAllTargetCreatures().Contains(chosenAlly))
								return;
							
							var willSaveResult = await CommonSpellEffects.RollSavingThrowAsync(qEffect.Owner, spell, Defense.Will, caster.ClassOrSpellDC());
							
							await CommonSpellEffects.DealBasicDamage(spell, caster, qEffect.Owner, willSaveResult, $"{damage}", DamageKind.Mental);
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
					[WitchLoader.ModName, Trait.Focus, THex, Trait.Manipulate, Trait.Mental, WitchLoader.TWitch, Trait.Uncommon],
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
							target.WeaknessAndResistance.Weaknesses.Add(new SpecialResistance(element.ToString(), (action, kind) => 
								(action != null && action.HasTrait(element)) || (kind == DamageKind.Fire && element == Trait.Fire), 
								(spellLevel + 1) / 2, null));
						}
					};
					
					target.AddQEffect(effect);
					caster.AddQEffect(QEffect.Sustaining(spell, effect));
				})
				.WithHeighteningNumerical(spellLevel, 1, inCombat, 2, $"Increase the weakness by 1.")
				.WithHexCasting();
		});
	
	public static SpellId BloodWard = ModManager.RegisterNewSpell("Blood Ward", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.BloodVendetta, "Blood Ward",
					[WitchLoader.ModName, Trait.Focus, THex, Trait.Manipulate, WitchLoader.TWitch, Trait.Uncommon],
					"Your patron's aegis descends to shield a target from harm.",
					$"Choose one creature trait from the following:  aberration, animal, beast, celestial, construct, dragon, elemental, fey, fiend, fungus, monitor, ooze, plant, or undead. The target gains a +1 (or +2 when cast at 5th level) status bonus to its saving throws and AC against creatures with that trait.",
					Target.RangedFriend(6), spellLevel, null)
				.WithActionCost(1)
				.WithSoundEffect(SfxName.ArmorDon)
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					var prompt = await caster.AskForChoiceAmongButtons(IllustrationName.BloodVendetta,
						$"Which creature trait should {target.Name} be defended against?",
						[ "aberration", "animal", "beast", "celestial", "construct", "dragon", "elemental", "fey", "fiend", "fungus", "monitor", "ooze", "plant", "undead"]);

					var trait = prompt.Index switch
					{
						0 => Trait.Aberration,
						1 => Trait.Animal,
						2 => Trait.Beast,
						3 => Trait.Celestial,
						4 => Trait.Construct,
						5 => Trait.Dragon,
						6 => Trait.Elemental,
						7 => Trait.Fey,
						8 => Trait.Fiend,
						9 => Trait.Fungus,
						10 => Trait.Monitor,
						11 => Trait.Ooze,
						12 => Trait.Plant,
						13 => Trait.Undead,
						_ => Trait.Uncommon
					};

					var value = spellLevel >= 5 ? 2 : 1;

					var effect = new QEffect("Blood ward", $"You have +1 to defenses against {trait}s", ExpirationCondition.ExpiresAtEndOfSourcesTurn, source: caster, illustration: IllustrationName.BloodVendetta)
					{
						CannotExpireThisTurn = true,
						BonusToDefenses = (effect, action, defense) => action != null && action.Owner.HasTrait(trait) ? new Bonus(value, BonusType.Status, "Blood Ward") : null
					};
					
					target.AddQEffect(effect);
					caster.AddQEffect(QEffect.Sustaining(spell, effect));
				})
				.WithHeightenedAtSpecificLevel(spellLevel, 5, inCombat, "The status bonus increases to +2")
				.WithHexCasting();
		});

	public static SpellId GougingClaw = ModManager.RegisterNewSpell("Gouging Claw", 0,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.MagicFang, "Gouging Claw",
					[
						WitchLoader.ModName, 
						Trait.Attack,
						Trait.Cantrip,
						Trait.Manipulate,
						Trait.Morph,
						Trait.Primal,
					], "You temporarily morph your limb into a clawed appendage.",
					$"Make a melee spell attack roll. If you hit, you deal your choice of {S.HeightenedVariable(spellLevel, 1)}d6 slashing or piercing damage (whichever is better), plus {S.HeightenedVariable(spellLevel + 1, 2)} persistent bleed damage. On a critical success, you deal double damage and double bleed damage.",
					Target.Touch(), spellLevel, null)
				.WithSpellAttackRoll()
				.WithSoundEffect(SfxName.AcidSplash)
				.WithEffectOnEachTarget(async (spell, caster, target, checkResult) =>
				{
					var baseDamageKind =
						target.WeaknessAndResistance.IsDamageKindSameAsOrBetterAgainstMe(DamageKind.Piercing,
							[DamageKind.Slashing])
							? DamageKind.Piercing
							: DamageKind.Slashing;
					
					await CommonSpellEffects.DealAttackRollDamage(spell, caster, target, checkResult,
						spellLevel + "d6", baseDamageKind);
					
					if (checkResult < CheckResult.Success)
						return;

					var persistentDamage = checkResult == CheckResult.CriticalSuccess ? 2 * (spellLevel + 1) : spellLevel + 1;
					
					target.AddQEffect(QEffect.PersistentDamage(persistentDamage.ToString(), DamageKind.Bleed));
				}).WithHeighteningNumerical(spellLevel, 1, inCombat, 1,
					"The damage increases by 1d6 and the persistent bleed damage increases by 1.");
		});

	public static SpellId FinalSacrifice = ModManager.RegisterNewSpell("Final Sacrifice", 2,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.Fireball, "Final Sacrifice", [
					Trait.Evocation,
					Trait.Fire,
					Trait.Arcane,
					Trait.Primal,
					Trait.Divine,
					Trait.Occult,
					Trait.Mod
				], 
				"You channel disruptive energies through the bond between you and your minion, causing it to violently explode.", 
				$"The target is immediately slain, and the explosion deals {S.HeightenedVariable(2 + (spellLevel * 2), 6)}d6 fire damage (basic Reflex save mitigates) to creatures within 20 feet of it. If the target has the cold or water trait, the spell deals cold damage and has the cold trait instead of the fire trait.",
				Target.RangedFriend(24).WithAdditionalConditionOnTargetCreature((self, ally) =>
				{
					if (ally.HasTrait(Trait.Summoned) && ally.FindQEffect(QEffectId.SummonedBy)?.Source == self || 
					    DeployableFamiliarTag.FindMaster(ally) == self)
						return Usability.Usable;

					return Usability.NotUsableOnThisCreature("Not your minion.");
				}), 
				spellLevel, 
				null)
				.WithSoundEffect(SfxName.Fireball)
				.WithEffectOnEachTarget(async (spell, caster, minion, _) =>
				{
					var animationTiles = minion.Battle.Map.AllTiles
						.Where(t => t.DistanceTo(minion.Space.CenterTile) <= 4).ToList();
					await CommonAnimations.CreateConeAnimation(minion.Battle, minion.Space.CenterVector, animationTiles, spell.ProjectileCount, spell.ProjectileKind, spell.ProjectileIllustration);
					
					var damageKind = DamageKind.Fire;
					if (minion.HasTrait(Trait.Cold) || minion.HasTrait(Trait.Water))
						damageKind = DamageKind.Cold;
					
					var othersInRange =
						minion.Battle.AllCreatures.Where(c => c != minion && c.DistanceTo(minion) < 4);
					
					foreach (var creatureInRange in othersInRange)
					{
						var result  = await CommonSpellEffects.RollSpellSavingThrowAsync(creatureInRange, spell, Defense.Reflex);
						
						await CommonSpellEffects.DealBasicDamage(spell, caster, creatureInRange, result, 2 + (2 * spellLevel) + "d6",
							damageKind);
					}

					minion.Die();
				});
		});
	
	public static SpellId GustOfWind = ModManager.RegisterNewSpell("Gust Of Wind", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.ElementalBlastAir, "Gust of Wind", [
					Trait.Evocation,
					Trait.Air,
					Trait.Concentrate,
					Trait.Manipulate,
					Trait.Arcane,
					Trait.Primal,
					Trait.Mod
				], 
				"A violent wind issues forth from your palm, blowing from the point where you are when you Cast the Spell to the line's opposite end.", 
				"{b}Duration:{/b} Until the start of your next turn.\nLarge or smaller creatures in the area must attempt a Fortitude save. Large or smaller creatures that later move into the gust must attempt the save on entering.\n"
				+ $"{S.FourDegreesOfSuccess("The target is unaffected.", "The creature can't move against the wind.", "The creature is knocked prone. If it was flying, it takes the effects of critical failure instead.", "The creature is pushed 30 feet in the wind's direction, knocked prone, and takes 2d6 bludgeoning damage.")}",
				Target.Line(12), 
				spellLevel, 
				null)
				.WithSoundEffect(SfxName.AirSpell)
				.WithEffectOnChosenTargets(async (spell, caster, chosenTargets) =>
				{
					QEffect effect = new QEffect(ExpirationCondition.ExpiresAtStartOfSourcesTurn)
					{
						Source = caster
					};
					var spawnedZone = Zone.SpawnStaticAndApply(effect, chosenTargets.ChosenTiles.Where(tl => !tl.AlwaysBlocksMovement).ToList(), async zone =>
					{	
						zone.TileEffectCreator = (Func<Tile, TileQEffect>) (tile => new TileQEffect(tile)
						{
							Illustration = IllustrationName.WhirlwindStrike,
							VisibleDescription = $"{{b}}Gust of Wind.{{/b}} A creature that enters must make a Fortitude save. {S.FourDegreesOfSuccess("The target is unaffected.", "The creature can't move against the wind.", "The creature is knocked prone. If it was flying, it takes the effects of critical failure instead.", "The creature is pushed 30 feet in the wind's direction, knocked prone, and takes 2d6 bludgeoning damage.")}"
						});

						zone.AfterCreatureEnters = creature => MakeSavingThrow(creature, zone);
					});
					
					foreach (var affectedTile in spawnedZone.AffectedTiles)
					{
						affectedTile.FoggyTerrain = false;
					}
					
					foreach (var creature in spawnedZone.CreaturesInZone)
					{
						await MakeSavingThrow(creature, spawnedZone);
					}

					async Task MakeSavingThrow(Creature entrant, Zone zone)
					{
						if (entrant.Space.SizeCategory > 2) return;

						var saveResult = await CommonSpellEffects.RollSavingThrowAsync(entrant, spell, Defense.Fortitude, caster.ClassOrSpellDC());

						if (saveResult == CheckResult.CriticalSuccess) return;

						if (saveResult == CheckResult.Success)
						{
							var immobilized = QEffect.Immobilized();
							immobilized.ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction;
							entrant.AddQEffect(immobilized);

							var hazardousEffect = new TileQEffect { TransformsTileIntoHazardousTerrain = true, ExpiresAt = ExpirationCondition.Never };

							foreach (var affectedTile in zone.AffectedTiles)
							{
								affectedTile.AddQEffect(hazardousEffect);
							}

							entrant.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn) { WhenExpires = _ => hazardousEffect.ExpiresAt = ExpirationCondition.Immediately });

							return;
						}

						await entrant.FallProne();
						if (saveResult == CheckResult.Failure && !entrant.HasEffect(QEffectId.Flying)) return;

						await caster.PushCreature(entrant, 6);
						await CommonSpellEffects.DealDirectDamage(spell, new SimpleDiceFormula(2, Dice.D6, spell.Name), entrant, CheckResult.CriticalFailure, DamageKind.Bludgeoning);
					}
					
					caster.AddQEffect(effect);
				});
		});
	
	// public static SpellId DeceiverCloak = RegisterNotImplementedSpell("DeceiverCloak", true, false);
	// public static SpellId MadMonkeys = RegisterNotImplementedSpell("MadMonkeys", false, false);
	// public static SpellId MaliciousShadow = RegisterNotImplementedSpell("MaliciousShadow", true, false);
	// public static SpellId PersonalBlizzard = RegisterNotImplementedSpell("PersonalBlizzard", true, false);
	// public static SpellId WallOfWind = RegisterNotImplementedSpell("WallOfWind", false, false);
	
	private static CombatAction WithHexCasting(this CombatAction combatAction) => combatAction.WithEffectOnEachTarget( 
		async (spell, caster, target, result) =>
	{
		if (!caster.HasEffect(QHexCasted))
			caster.AddQEffect(QHexCasted);
	});

	private static SpellId RegisterNotImplementedSpell(string title, bool isHex, bool isCantrip)
	{
		var minLevel = isCantrip ? 0 : 1;
		return ModManager.RegisterNewSpell(title + " (Not implemented)", minLevel,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			var spell = Spells.CreateModern(IllustrationName.DawnsburyDaysPureLogo, title + " (Not Implemented)",
					[WitchLoader.ModName, Trait.Concentrate],
					"",
					"This spell has not been implemented.",
					Target.Self(),
					spellLevel, null)
				.WithActionCost(0)
				.WithNotImplemented();

			if (isCantrip)
				spell.Traits.Add(Trait.Cantrip);
			
			if (isHex)
			{
				spell.Traits.Add(Trait.Focus);
				spell.Traits.Add(THex);
				spell.Traits.Add(WitchLoader.TWitch);
				spell.Traits.Add(Trait.Uncommon);
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