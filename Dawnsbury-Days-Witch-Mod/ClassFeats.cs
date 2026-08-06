using Dawnsbury.Audio;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Campaign.Path.CampaignStops;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.ContextMenu;
using Dawnsbury.Display.Controls;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.Classes.Witch;

public static class ClassFeats
{
	public static Trait TSympStrike = ModManager.RegisterTrait("SympatheticStrike", new TraitProperties("", relevant: false));
	public static Trait TBasicLesson = ModManager.RegisterTrait("BasicLesson", new TraitProperties("", relevant: false));
	public static Trait TGreaterLesson = ModManager.RegisterTrait("GreaterLesson", new TraitProperties("", relevant: false));
	public static QEffect QSympStrikeUsed = new()
	{
		ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn
	};

	public static FeatName FNCeremonialKnife = ModManager.RegisterFeatName("CeremonialKnife", "Ceremonial Knife");
	public static FeatName FNCauldron = ModManager.RegisterFeatName("Cauldron");
	
	public static IEnumerable<Feat> CreateFeats()
	{
		HashSet<FeatName> addWitchTraitTo = [
			FeatName.DawnsburyEnhancedFamiliar,
			FeatName.Counterspell,
			FeatName.ReachSpell,
			FeatName.WidenSpell,
			FeatName.SteadySpellcastingMain
		];

		foreach (var feat in AllFeats.All.FindAll(feat => addWitchTraitTo.Contains(feat.FeatName)))
		{
			if (feat is not TrueFeat tf)
				continue;

			tf.WithAllowsForAdditionalClassTrait(WitchModData.Traits.Witch);
		}

		// TODO: incredible familiar not in base game?
		// Incredible Familiar does not initially have a class prerequisite, so WithAllowsForAdditionalClassTrait wouldn't work
		// var incredibleFamiliar = AllFeats.All.Find(feat => feat.FeatName == Familiars.ClassFeats.FNIncredibleFamiliar);
		// incredibleFamiliar?.Prerequisites.Add(new ClassPrerequisite([WitchLoader.TWitch]));

		yield return new TrueFeat(ModManager.RegisterFeatName("Cackle"), 1,
			"Your patron’s power fills you with confidence, letting you sustain a magical working even as a quick burst of laughter leaves your lips.",
			"You learn the cackle hex.", [WitchModData.Traits.Witch])
			.WithOnSheet(sheet => sheet.AddFocusSpellAndFocusPoint(WitchModData.Traits.Hex, Ability.Intelligence, WitchSpells.Cackle)
		);

		yield return new TrueFeat(ModManager.RegisterFeatName("CantripExpansionWitch", "Cantrip Expansion"), 1,
			"A greater understanding of your magic broadens your range of simple spells.",
			"You can prepare two additional cantrips each day.",
			[WitchModData.Traits.Witch]).WithOnSheet(values =>
		{
			values.PreparedSpells.GetValueOrDefault(WitchModData.Traits.Witch)?.Slots
				.Add(new FreePreparedSpellSlot(0, "CantripExpansion1"));
			values.PreparedSpells.GetValueOrDefault(WitchModData.Traits.Witch)?.Slots
				.Add(new FreePreparedSpellSlot(0, "CantripExpansion2"));
		});

		yield return new TrueFeat(FNCauldron, 1,
			"You can brew magic in your cauldron, creating useful magical concoctions.",
			"During your daily preparations, you can create one 1st-level temporary oil or potion. At 4th level and every 2 levels after that, the maximum level of the oil or potion becomes equal to your level. A temporary oil or potion has no value, and you can only have one at a time.",
			[WitchModData.Traits.Witch]);
		
		ItemModifications.RegisterItemModification("cauldron-crafted",
			modification => "cauldron-crafted",
			(deserialization, kind) =>
			{
				if (!deserialization.StartsWith("cauldron-crafted"))
					return null;

				return new ItemModification(kind)
				{
					ModifyItem = crafted =>
					{
						crafted.ProsaicName += " (Temporary)";
						crafted.Price = 0;
					}
				};
			});
		
		var cauldronFormulas = Items.ShopItems
			.Where(item => item.HasTrait(Trait.Potion) || item.HasTrait(Trait.Oil));

		foreach (var formula in cauldronFormulas)
		{
			InventoryContextMenu.Options.Add(new InventoryContextMenuOption(
				(Func<InventoryItemSlot, Item, Inventory, ContextMenuItem[]>)
				((itemSlot, _, inventory) =>
				{
					if (CampaignState.Instance != null && CampaignState.Instance.CurrentStop is not LongRestCampaignStop 
					                                   && CampaignState.Instance.CurrentStop is not NarratorStop 
					                                   && CampaignState.Instance.CurrentStop is not LevelUpStop)
						return null;

					if (itemSlot.Item != null)
						return null;

					if (itemSlot.InventoryItemSlotKind == InventoryItemSlotKind.Armor)
						return null;
				
					CharacterSheet characterSheet = itemSlot.CharacterSheet;
					if (characterSheet == null || !characterSheet.Calculated.HasFeat(FNCauldron))
						return null;

					int charLevel = characterSheet.EditingInventoryAtLevel;
					
					if (charLevel < 4 && formula.Level > 1)
						return null;

					if (charLevel >= 4)
					{
						int levelToCheck = charLevel;
						if (levelToCheck % 2 != 0)
							levelToCheck -= 1;

						if (formula.Level > levelToCheck)
							return null;
					}

					return
					[
						new ContextMenuItem(formula.Illustration, $"Craft temporary {formula.ProsaicName}",
							$"Creates a {formula.ProsaicName} that lasts until the next long rest or until used.",
							() =>
							{
								if (ModManager.TryParse("cauldron-crafted", out ItemModificationKind kind))
								{
									Item? previouslyCrafted;
									do
									{
										previouslyCrafted =
											inventory.RemoveFirstInventoryItem(item => 
												item.ItemModifications.Any(mod => mod.Kind == kind));
									} while (previouslyCrafted != null);
								}
								
								var crafted = Items.CreateNew(formula.ItemName);
								crafted.WithModification(ItemModification.Create("cauldron-crafted"));
								
								itemSlot.ReplaceSelf(crafted);
							})
					];
				})));
		}

		var nails = new Item(IllustrationName.DragonClaws, "Eldritch Claws", Trait.Brawling, Trait.Agile,
				Trait.Unarmed, TSympStrike)
			.WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Slashing))
			.WithSoundEffect(SfxName.ScratchFlesh);
		var nailsFeat = new TrueFeat(ModManager.RegisterFeatName("WitchArmamentsNails", "Witch's Armaments (Eldritch Nails)"), 1,
				"Your patron’s power changes your body to ensure you are never defenseless.",
				"Your nails are supernaturally long and sharp. You gain a nails unarmed attack that deals 1d6 slashing damage, is in the brawling group, and has the agile and unarmed traits.",
				[WitchModData.Traits.Witch])
			.WithOnCreature(creature => creature.WithAdditionalUnarmedStrike(nails));
		yield return nailsFeat;

		var teeth = new Item(IllustrationName.Jaws, "Iron Teeth", Trait.Brawling, TSympStrike)
			.WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Piercing)).WithSoundEffect(SfxName.BiteApple);
		var teethFeat = new TrueFeat(ModManager.RegisterFeatName("WitchArmamentsTeeth", "Witch's Armaments (Iron Teeth)"), 1,
				"Your patron’s power changes your body to ensure you are never defenseless.",
				"With a click of your jaw, your teeth transform into long metallic points. You gain a jaws unarmed attack that deals 1d8 piercing damage and is in the brawling group.",
				[WitchModData.Traits.Witch])
			.WithOnCreature(creature =>
			{
				creature.WithAdditionalUnarmedStrike(teeth);
			});
		yield return teethFeat;

		var hair = new Item(IllustrationName.BlackTentacles, "Living Hair", Trait.Brawling, Trait.Agile,
				Trait.Disarm, Trait.Finesse, Trait.Trip, Trait.Unarmed, TSympStrike)
			.WithWeaponProperties(new WeaponProperties("1d4", DamageKind.Bludgeoning))
			.WithSoundEffect(SfxName.BiteApple); // TODO: sound effect
		var hairFeat =  new TrueFeat(ModManager.RegisterFeatName("WitchArmamentsHair", "Witch's Armaments (Living Hair)"), 1,
				"Your patron’s power changes your body to ensure you are never defenseless.",
				"You can instantly grow or shrink your hair, eyebrows, beard, or mustache by up to several feet and manipulate your hair for use as a weapon, though your control isn’t fine enough for more dexterous tasks. You gain a hair unarmed attack that deals 1d4 bludgeoning damage; is in the brawling group; and has the agile, disarm, finesse, trip, and unarmed traits.",
				[WitchModData.Traits.Witch])
			.WithOnCreature(creature => creature.WithAdditionalUnarmedStrike(hair));
		yield return hairFeat;

		yield return new TrueFeat(ModManager.RegisterFeatName("Sympathetic Strike"), 4,
				"You collect your patron’s magic into one of your witch armaments, causing them to shine with runes, light, or another signifier of your patron.",
				"Once per round, you can make an unarmed Strike with one with your witch’s armaments. If you hit, you establish a sympathetic link with the target, making it easier for your patron to affect them. Until the beginning of your next turn, the target takes a –1 circumstance penalty to its saves against your hexes, or a –2 penalty if the triggering Strike was a critical hit.",
				[WitchModData.Traits.Witch])
			.WithPrerequisite(new Prerequisite(sheet => sheet.HasFeat(nailsFeat) || sheet.HasFeat(teethFeat) || sheet.HasFeat(hairFeat), "You must have the feat Witch's Armaments."))
			.WithOnCreature(witch =>
			{
				var calculated = witch.PersistentCharacterSheet?.Calculated;
				if (calculated == null)
					return;
				
				witch.AddQEffect(new QEffect
				{
					ProvideStrikeModifier = item =>
					{
						if (witch.HasEffect(QSympStrikeUsed))
							return null;
							
						if (!item.HasTrait(TSympStrike))
							return null;
							
						var strikeModifiers = new StrikeModifiers
						{
							OnEachTarget = async (caster, target, result) =>
							{
								caster.AddQEffect(QSympStrikeUsed);
								switch (result)
								{
									case < CheckResult.Success:
										return;
									case CheckResult.Success:
										target.AddQEffect(new QEffect("Sympathetic Link", "You take a –1 circumstance penalty to saves against hexes", ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster)
										{
											BonusToDefenses = (effect, action, defense) => action != null && action.HasTrait(WitchModData.Traits.Hex) ? new Bonus(-1, BonusType.Circumstance, "Sympathetic Link") : null
										});
										break;
									case CheckResult.CriticalSuccess:
										target.AddQEffect(new QEffect("Sympathetic Link", "You take a –2 circumstance penalty to saves against hexes", ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster)
										{
											BonusToDefenses = (effect, action, defense) => action != null && action.HasTrait(WitchModData.Traits.Hex) ? new Bonus(-2, BonusType.Circumstance, "Sympathetic Link") : null
										});
										break;
								}
							}
						};
							
						var strike = witch.CreateStrike(item, strikeModifiers: strikeModifiers);
						strike.Name = item.Name + " (Sympathetic Strike)";
						strike.Traits = new Traits([..strike.Traits], strike);
						return strike;
					}
				});
			});

		yield return new TrueFeat(ModManager.RegisterFeatName("Basic Lesson"), 2,
			"",
			"",
			[WitchModData.Traits.Witch],
			[
				CreateLesson(TBasicLesson, "Dreams", "Dreams can be a window to greater insights.", WitchSpells.VeilOfDreams, SpellId.Sleep),
				CreateLesson(TBasicLesson, "Life", "Life can be shared.", WitchSpells.LifeBoost, WitchSpells.SpiritLink),
				CreateLesson(TBasicLesson, "Protection", "An ounce of protection is worth a pound of cure.", WitchSpells.BloodWard, SpellId.MageArmor),
				CreateLessonElements(TBasicLesson, "Elements", "Natural disasters and inclement weather hold more power than the mightiest creature.", WitchSpells.ElementalBetrayal, [SpellId.BurningHands, WitchSpells.GustOfWind, SpellId.HydraulicPush, SpellId.PummelingRubble]),
				CreateLesson(TBasicLesson, "Vengeance", "Suffer not even the smallest slights.", WitchSpells.NeedleOfVengeance, SpellId.PhantomPain)
			]);

		// yield return new TrueFeat(ModManager.RegisterFeatName("Greater Lesson"), 4,
		// 		"",
		// 		"",
		// 		[WitchLoader.TWitch],
		// 		[
		// 			CreateLesson(TGreaterLesson, "Mischief", "Nothing's wrong with some mischief, now and then.", WitchSpells.DeceiverCloak, WitchSpells.MadMonkeys),
		// 			CreateLesson(TGreaterLesson, "Shadow", "A shadow is far from empty — it contains something of the person who casts it.", WitchSpells.MaliciousShadow, SpellId.ChillingDarkness),
		// 			CreateLesson(TGreaterLesson, "Snow", "Emulate snow, for it can snuff out life despite its gentleness.", WitchSpells.PersonalBlizzard, WitchSpells.WallOfWind)
		// 		]);

		yield return new TrueFeat(FNCeremonialKnife, 6,
				"You have prepared a special knife to direct energies when spellcasting.",
				"In your daily preparations, you can mark a weapon that you own and is from the knife group. This causes the knife to function as a magic wand, containing any one 1st-rank spell your familiar knows. {i}(To mark it, right-click a knife in your inventory and choose ‘Mark as ceremonial knife.’){/i} You, and only you, can Activate the knife to Cast the Spell within it, as normal for a wand. You can attempt to overcharge the knife, and this can break or destroy the knife as normal. You can have only one ceremonial knife at a time.\n\nAt 8th level, and every 2 levels thereafter, the maximum rank of spell your ceremonial knife can hold increases by 1.",
				[WitchModData.Traits.Witch])
			.WithOnSheet(values =>
			{
				int maxSpellLevel = values.CurrentLevel <= 6 ? 1 : 1 + ((values.CurrentLevel - 6) / 2);
				values.AddSelectionOption(new CeremonialKnifeOption("CeremonialKnifeSpell", "Ceremonial Knife Spell", 6,
					maxSpellLevel));
			});

		ItemModifications.RegisterItemModification("ceremonial-knife-spell",
			modification => "ceremonial-knife-spell-" + modification.Tag,
			(deserialization, kind) =>
			{
				if (!deserialization.StartsWith("ceremonial-knife-spell-"))
					return null;
				
				string spellId = deserialization.Substring("ceremonial-knife-spell-".Length);
				if (spellId.Length == 0)
					return null;
				
				if (!ModManager.TryParse<SpellId>(spellId, out var trueSpellId))
					return null;

				return new ItemModification(kind)
				{
					Tag = spellId,
					ModifyItem = knife =>
					{
						knife.Illustration =
							new SuperimposedIllustration(IllustrationName.MagicCircle150, knife.Illustration);
						
						knife.ProvidesItemAction = (self, wand) =>
						{
							if (self.Spellcasting?.Sources.FirstOrDefault(src =>
								    src.ClassOfOrigin == WitchModData.Traits.Witch) == null)
								return null;
							
							Spell? spell = AllSpells.All.FirstOrDefault(s => s.SpellId == trueSpellId);
							if (spell == null)
								return null;

							if (knife.IsUsedUp)
								return null;

							bool usedOnce =
								self.PersistentUsedUpResources.UsedUpActions.Contains("CeremonialKnife");

							Spellcasting? spellcasting = self.Spellcasting;
							SpellcastingSource? spellcastingSource = spellcasting?.Sources
								.Where(src => src.ClassOfOrigin != Trait.UsesTrickMagicItem)
								.FirstOrDefault((Func<SpellcastingSource, bool>)(src =>
									spell.HasTrait(src.SpellcastingTradition)));
							CombatAction spellInCombat = AllSpells.CreateSpellInCombat(trueSpellId, self,
								spell.SpellLevel, spellcastingSource?.ClassOfOrigin ?? Trait.Spell);
							spellInCombat.SpellcastingSource = spellcastingSource;
							spellInCombat.CastFromScroll = wand;
							spellInCombat.Illustration =
								new SideBySideIllustration(wand.Illustration, spell.Illustration);
							
							spellInCombat.WithEffectOnSelf(creature =>
							{
								if (!creature.PersistentUsedUpResources.UsedUpActions.Contains("CeremonialKnife"))
									creature.PersistentUsedUpResources.UsedUpActions.Add("CeremonialKnife");
							});

							if (usedOnce)
							{
								spellInCombat.Name += " (Overcharge)";
								
								spellInCombat.WithEffectOnSelf(creature =>
								{
									// Roll DC10 flat check. On success, mark weapon as broken. On failure, destroy weapon
									// If destroying the weapon, detach its runes first?
									creature.AddQEffect(new QEffect()
									{
										PreventTakingAction = action =>
										{
											if (action.Item == knife)
												return "This knife has been broken by overcharging it with a spell. It will be repaired upon your next long rest.";

											return null;
										}
									});
									
									knife.UseUp();
									
									creature.Overhead("Knife Breaks", Color.Black, $"{creature.Name}'s knife breaks from overcharging it with a spell. It will be repaired upon your next long rest.");
								});
							}

							return new ActionPossibility(spellInCombat);
						};
					},
					UnmodifyItem = knife =>
					{
						if (knife.Illustration is SuperimposedIllustration superimposedIllustration)
							knife.Illustration = superimposedIllustration.Top;

						knife.ProvidesItemAction = null;
					}
				};
			});
		
		InventoryContextMenu.Options.Add(new InventoryContextMenuOption(
			(Func<InventoryItemSlot, Item, Inventory, ContextMenuItem[]>)
			((itemSlot, knife, inventory) =>
			{
				if (CampaignState.Instance != null && CampaignState.Instance.CurrentStop is not LongRestCampaignStop 
				                                   && CampaignState.Instance.CurrentStop is not NarratorStop 
				                                   && CampaignState.Instance.CurrentStop is not LevelUpStop)
					return null;
				
				CharacterSheet characterSheet = itemSlot.CharacterSheet;
				if (characterSheet == null || !characterSheet.Calculated.HasFeat(FNCeremonialKnife))
					return null;

				if (knife == null)
					return null;

				if (!knife.HasTrait(Trait.Knife))
					return null;

				return
				[
					new ContextMenuItem(IllustrationName.Dagger, "Mark as ceremonial knife",
						"Adds the ability for this knife to function as a magic wand, containing one spell your familiar knows.",
						(Action)(() =>
						{
							SpellId? spellId =
								characterSheet.Calculated.GetTagOrNull<SpellId>("CeremonialKnifeSpell");
							if (spellId == null)
								return;

							Spell? spell = AllSpells.All.FirstOrDefault(s => s.SpellId == spellId.Value);
							if (spell == null)
								return;

							if (ModManager.TryParse("ceremonial-knife-spell", out ItemModificationKind kind))
							{
								foreach (var item in inventory.AllItems)
								{
									item.WithoutModification(kind);
								}
							}

							knife.WithModification(ItemModification.Create($"ceremonial-knife-spell-{((SpellId)spellId).ToStringOrTechnical()}"));
						}))
				];
			})));

		// yield return new TrueFeat(ModManager.RegisterFeatName("WitchBottle", "Witch's Bottle"), 8,
		// 	"",
		// 	"",
		// 	[WitchLoader.TWitch])
		// 	.WithPrerequisite(cauldron, "Cauldron");
	}

	private static Feat CreateLesson(Trait rank, string name, string flavorText, SpellId hex, SpellId spell)
	{
		return new Feat(ModManager.RegisterFeatName($"Lesson{name}", $"Lesson of {name}"), flavorText,
			$"You gain the {AllSpells.CreateSpellLink(hex, WitchModData.Traits.Hex)} hex, and you add {AllSpells.CreateSpellLink(spell, WitchModData.Traits.Witch)} to your spell list.",
			[rank], null)
			.WithOnSheet(sheet =>
			{
				sheet.AddFocusSpellAndFocusPoint(WitchModData.Traits.Hex, Ability.Intelligence, hex);
				sheet.PreparedSpells[WitchModData.Traits.Witch].AdditionalPreparableSpells.Add(spell);
			});
	}

	private static Feat CreateLessonElements(Trait rank, string name, string flavorText, SpellId hex, SpellId[] spells)
	{
		return new Feat(ModManager.RegisterFeatName($"Lesson{name}", $"Lesson of {name}"), flavorText,
				$"You gain the {AllSpells.CreateSpellLink(hex, WitchModData.Traits.Hex)} hex, and you add {AllSpells.CreateSpellLink(spells[0], WitchModData.Traits.Witch)}, {AllSpells.CreateSpellLink(spells[1], WitchModData.Traits.Witch)}, {AllSpells.CreateSpellLink(spells[2], WitchModData.Traits.Witch)} and {AllSpells.CreateSpellLink(spells[3], WitchModData.Traits.Witch)} to your spell list.",
				[rank], null)
			.WithOnSheet(sheet =>
			{
				sheet.AddFocusSpellAndFocusPoint(WitchModData.Traits.Hex, Ability.Intelligence, hex);
				sheet.PreparedSpells[WitchModData.Traits.Witch].AdditionalPreparableSpells.AddRange(spells);
			});
	}
}