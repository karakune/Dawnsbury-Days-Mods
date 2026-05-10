using System.Reflection;
using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.Aesir;

public static class AesirLoader
{
	[DawnsburyDaysModMainMethod]
	public static void LoadMod()
	{
		ModManager.AddFeat(
			new TrueFeat(
					ModManager.RegisterFeatName("NephilimBattleblooded", "Battleblooded"),
					1,
					"You descend from those whose lives were touched by legendary einherjars, who were crafted from the souls of mighty warriors slain in battle and chosen by valkyries to serve as foot soldiers to many gods. You may even be the distant mortal descendant of an actual valkyrie.", 
					"You are trained in Intimidation. If you were already trained in Intimidation (from your background or class, for example), you instead become trained in a skill of your choice. You also gain the Intimidating Glare skill feat.", 
					[Trait.Tiefling]
			)
			.WithOnSheet(sheet =>
			{
				sheet.TrainInThisOrSubstitute(Skill.Intimidation);
				sheet.GrantFeat(FeatName.IntimidatingGlare);
			})
		);

		List<SpellId> grantedSpells =
		[
			SpellId.Shield,
			SpellId.MageArmor,
			SpellId.SpiritualWeapon,
			SpellId.Heroism,
			SealFate,
			// Invoke Spirits goes here
			SpellId.BlindingFury,
			SpellId.FingerOfDeath,
			// SpellId.SpiritSong,
			// SpellId.Massacre
		];
		if (ModManager.TryParse("InvokeSpirits", out SpellId invokeSpiritsId))
			grantedSpells.Insert(5, invokeSpiritsId);

		var bloodline = new Bloodline(ModManager.RegisterFeatName("AesirBloodline", "Aesir"),
				"One of your ancestors was touched by an aesir, and now the ringing of steel against steel and the constant call to battle echo through your blood.",
				Trait.Divine,
				BarbedSpear,
				WingsOfTheValkyrie,
				LetNotTheFallenRest,
				grantedSpells.ToArray(),
				[FeatName.Intimidation, FeatName.Religion]
			);
		bloodline.WithBloodMagic("You bring down a bolt of lightning to damage a foe or energize yourself. Either you gain a +2 status bonus to Reflex saving throws for 1 round, or a target takes 1 electricity damage per spell rank.",
			friendlyEffect =>
			{
				friendlyEffect.BonusToDefenses = (effect, action, defense) => defense == Defense.Reflex ? new Bonus(2, BonusType.Status, "Aesir Bloodline blood magic") : null;
				friendlyEffect.Description = "You have +2 to Reflex saving throws.";
			},
			null,
			permanentEffect =>
			{
				permanentEffect.YouDealDamageEvent = async (effect, damageEvent) =>
				{
					var activatesBloodMagicFunc = bloodline.GetType().GetMethod("ActivatesBloodMagic", BindingFlags.NonPublic | BindingFlags.Instance);
			
					bool activatesBloodMagic = (bool)(activatesBloodMagicFunc?.Invoke(bloodline, [damageEvent.CombatAction]) ?? false);
					
					if (damageEvent.CombatAction == null || !activatesBloodMagic)
						return;
					DiceFormula diceFormula = DiceFormula.FromText(damageEvent.CombatAction.SpellLevel.ToString(),
						"Blood magic (spell level)");
					KindedDamage? kindedDamage =
						damageEvent.KindedDamages.FirstOrDefault(kd => kd.DamageKind == DamageKind.Fire);
					if (kindedDamage?.DiceFormula != null)
						kindedDamage.DiceFormula = kindedDamage.DiceFormula.Add(diceFormula);
					else
						damageEvent.KindedDamages.Add(new KindedDamage(diceFormula, DamageKind.Electricity));
				};
			});
		
		AllFeats.GetFeatByFeatName(FeatName.Sorcerer).Subfeats!.Add(bloodline);	
	}
	
	public static SpellId BarbedSpear = ModManager.RegisterNewSpell("Barbed Spear", 1,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.Spear, "Barbed Spear",
					[Trait.Concentrate, Trait.Attack, Trait.Focus, Trait.Manipulate, Trait.Sorcerer, Trait.Uncommon],
					$"You conjure a vicious barbed spear and hurl it at a foe.",
					$"Make a ranged spell attack roll, dealing {S.HeightenedVariable(spellLevel, 1)}d8 piercing damage on a success and double damage on a critical success.\n\nThe spear remains lodged within a creature it hits, making the target clumsy 1 (or increasing its clumsy condition by 1 if it is already clumsy) for 1 minute or until the spear is removed with a successful Athletics check against your spell DC as an Interact action, whichever comes first.",
					Target.Ranged(6), spellLevel, null)
				.WithSpellAttackRoll()
				.WithActionCost(2)
				.WithSoundEffect(SfxName.Angelic)
				.WithEffectOnEachTarget(async (spell, caster, target, checkResult) =>
				{
					await CommonSpellEffects.DealAttackRollDamage(spell, caster, target, checkResult,
						spellLevel + "d8", DamageKind.Piercing);
					
					if (checkResult < CheckResult.Success)
						return;
					
					CommonSpellEffects.CumulativeClumsy(target, 1);
					QEffect removeSpear = new QEffect();
					removeSpear.ProvideContextualAction = effect => new ActionPossibility(
						new CombatAction(effect.Owner, IllustrationName.WinterBolt, "Remove spear",
								[Trait.Interact],
								"Make a DC " + caster.ClassOrSpellDC() +
								" Athletics check to pull out the spear encumbering you.\n\n{b}Success{/b} You reduce your Clumsy effect by 1.",
								Target.Self((_, _) => 1f)
							)
							.WithActiveRollSpecification(new ActiveRollSpecification(
								TaggedChecks.SkillCheck(Skill.Athletics), Checks.FlatDC(caster.ClassOrSpellDC())))
							.WithEffectOnEachTarget(async (_, _, _, result) =>
							{
								QEffect? clumsy = target.FindQEffect(QEffectId.Clumsy);
								
								if (clumsy == null)
								{
									removeSpear.ExpiresAt = ExpirationCondition.Immediately;	
									return;
								}

								if (result < CheckResult.Success)
									return;

								int value = clumsy.Value;
								target.RemoveAllQEffects(qf => qf.Id == QEffectId.Clumsy);
								if (value > 1)
									target.AddQEffect(QEffect.Clumsy(value - 1));

								removeSpear.ExpiresAt = ExpirationCondition.Immediately;
							})
					).WithPossibilityGroup("Remove debuff");
					target.AddQEffect(removeSpear);
				})
				.WithHeighteningNumerical(spellLevel, 1, inCombat, 1, "The damage increases by 1d8.");
		});
	
	public static SpellId WingsOfTheValkyrie = ModManager.RegisterNewSpell("Wings Of The Valkyrie", 3,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.Wing, "Wings Of The Valkyrie",
					[Trait.Concentrate, Trait.Focus, Trait.Manipulate, Trait.Sorcerer, Trait.Uncommon],
					$"Powerful feathered wings emerge from your back.",
					"{b}Duration: 3 rounds{/b}\n\nYou gain a fly speed for the duration. You can use these wings to attempt to Shove a creature; you don’t need a free hand to do so, and you can roll using your spell attack modifier instead of your Athletics skill for the check.",
					Target.Self(), spellLevel, null)
				.WithActionCost(2)
				.WithSoundEffect(SfxName.Angelic)
				.WithEffectOnEachTarget(async (spell, _, target, _) =>
				{
					QEffect flyingEffect = QEffect.Flying();
					if (spellLevel < 5)
					{
						flyingEffect.Value = 3;
						flyingEffect.ExpiresAt = ExpirationCondition.CountsDownAtEndOfYourTurn;
					}

					flyingEffect.ProvideContextualAction = effect => new ActionPossibility(
						new CombatAction(effect.Owner, IllustrationName.Wing, "Shove (wings)",
							[Trait.Basic, Trait.Attack, Trait.AttackDoesNotTargetAC, Trait.Spell],
							"Shove a creature, using the best of your Athletics or Spellcasting modifier.",
							Target.Touch().WithAdditionalConditionOnTargetCreature(new TargetMustNotBeTwoSizesAboveYouCreatureTargetingRequirement()))
							.WithSoundEffect(SfxName.Shove).WithActionId(ActionId.Shove)
							.WithSpellcastingSource(spell.SpellcastingSource)
							.WithActiveRollSpecification(new ActiveRollSpecification(TaggedChecks.BestRoll(TaggedChecks.SpellAttack(), TaggedChecks.SkillCheck(Skill.Athletics)), TaggedChecks.DefenseDC(Defense.Fortitude)))
							.WithEffectOnEachTarget(async (action, caster, creature, checkResult) =>
							{
								if (checkResult == CheckResult.CriticalFailure)
								{
									await caster.FallProne();
									return;
								}
								
								if (checkResult < CheckResult.Success)
									return;
								
								int squareCount = checkResult == CheckResult.CriticalSuccess ? 2 : 1;
								Tile previousPosition = creature.Space.TopLeftTile;
								await caster.PushCreature(creature, squareCount);
								Tile topLeftTile = creature.Space.TopLeftTile;
								if (Equals(previousPosition, topLeftTile))
									return;
								
								Point point = new Point(topLeftTile.X - previousPosition.X, topLeftTile.Y - previousPosition.Y);
								Tile yourNewPosition = caster.Battle.Map.GetTile(caster.Space.TopLeftTile.X + point.X, caster.Space.TopLeftTile.Y + point.Y)!;
								ICombatAction? combatAction = caster.Possibilities.CreateActions(false).FirstOrDefault(act => act.Action.ActionId == ActionId.Stride);
								++effect.Owner.Actions.ActionsLeft;
								bool flag3 = combatAction != null && (bool) combatAction.CanBeginToUse(effect.Owner);
								--effect.Owner.Actions.ActionsLeft;
								if (flag3)
								{
									if (await caster.Battle.AskForConfirmation(caster, IllustrationName.Shove, $"You shoved {creature}.\nStride after the target?", "Stride"))
										await caster.MoveTo(yourNewPosition, action, new MovementStyle()
										{
											MaximumSquares = 100,
											Shifting = true,
											ShortestPath = true
										});
								}
							})
					).WithPossibilityGroup("Maneuvers");
					target.AddQEffect(flyingEffect);
				})
				.WithHeightenedAtSpecificLevel(3, 5, inCombat, "The duration increases to the rest of the encounter.");
		});
	
	public static SpellId LetNotTheFallenRest = ModManager.RegisterNewSpell("Let Not The Fallen Rest", 5,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.Heal, "Let Not The Fallen Rest",
					[Trait.Concentrate, Trait.Focus, Trait.Healing, Trait.Manipulate, Trait.Sorcerer, Trait.Uncommon],
					$"You exhort your fallen comrades to return to the battle.",
					$"Each allied creature within the emanation whose dying value is 2 or less regains {S.HeightenedVariable(spellLevel, 4)}d10 Hit Points and can Stand as a free action, which doesn’t provoke reactions.",
					Target.AlliesOnlyEmanation(6), spellLevel, null)
				.WithActionCost(3)
				.WithSoundEffect(SfxName.Angelic)
				.WithEffectOnEachTarget(async (spell, caster, target, checkResult) =>
				{
					QEffect? dying = target.FindQEffect(QEffectId.Dying);
					QEffect? unconscious = target.FindQEffect(QEffectId.Unconscious);
					if (unconscious != null || (dying != null && dying.Value <= 2))
					{
						await target.HealAsync(3 + spellLevel + "d10",
							CombatAction.CreateSimple(target, "Let Not The Fallen Rest"));
					
						target.StandUp();
					}
				})
				.WithHeighteningNumerical(spellLevel, 1, inCombat, 1, "The healing increases by 1d10.");
		});
	
	public static SpellId SealFate = ModManager.RegisterNewSpell("Seal Fate", 4,
		(spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
		{
			return Spells.CreateModern(IllustrationName.DeathsCall, "Seal Fate",
					[Trait.Concentrate, Trait.Curse, Trait.Death, Trait.Manipulate, Trait.Necromancy],
					"You utter a curse that a creature will meet a certain end — a death by freezing, stabbing, or another means you devise.",
					$"Choose one type of damage from the following list: acid, bludgeoning, cold, electricity, fire, negative, piercing, slashing, or sonic. The effect is based on the target's Fortitude save.\n\n" +
					$"{S.FourDegreesOfSuccess("The target is unaffected.", $"The target gains weakness {S.HeightenedVariable(spellLevel / 2, 2)} to the chosen damage type until the end of your next turn.", "As success, but the duration is until the end of the encounter.", null)}",
					Target.Touch(), spellLevel, SpellSavingThrow.Standard(Defense.Fortitude))
				.WithActionCost(2)
				.WithSoundEffect(SfxName.DeathsCall)
				.WithEffectOnEachTarget(async (spell, caster, target, result) =>
				{
					if (result == CheckResult.CriticalSuccess)
						return;
					
					var prompt = await caster.AskForChoiceAmongButtons(IllustrationName.DeathsCall,
						$"Which damage type should {target.Name} be weak to?",
						[
							"acid", "bludgeoning", "cold", "electricity", "fire", "negative", "piercing", "slashing",
							"sonic"
						]);

					DamageKind damageKind = prompt.Index switch
					{
						0 => DamageKind.Acid,
						1 => DamageKind.Bludgeoning,
						2 => DamageKind.Cold,
						3 => DamageKind.Electricity,
						4 => DamageKind.Fire,
						5 => DamageKind.Negative,
						6 => DamageKind.Piercing,
						7 => DamageKind.Slashing,
						8 => DamageKind.Sonic,
						_ => DamageKind.Untyped
					};

					var effect = new QEffect("Sealed Fate",
						$"You have weakness {spellLevel / 2} to {damageKind}.",
						ExpirationCondition.ExpiresAtEndOfSourcesTurn, source: caster,
						IllustrationName.DeathsCall)
					{
						CannotExpireThisTurn = true,
						CountsAsADebuff = true,
						StateCheck = qEffect =>
						{
							target.WeaknessAndResistance.Weaknesses.Add(new SpecialResistance("Sealed Fate",
								(action, kind) => kind == damageKind, spellLevel / 2, null));
						}
					};

					if (result == CheckResult.Success)
					{
						effect.CannotExpireThisTurn = true;
						effect.ExpiresAt = ExpirationCondition.ExpiresAtEndOfSourcesTurn;	
					}
					else
					{
						effect.ExpiresAt = ExpirationCondition.Never;
					}

					target.AddQEffect(effect);
				})
				.WithHeighteningNumerical(spellLevel, 4, inCombat, 2, $"Increase the weakness by 1.");
		});
}