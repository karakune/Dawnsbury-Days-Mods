using System.Net.Mail;
using Dawnsbury.Campaign.LongTerm;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.DeployableFamiliars;

public class DeployableFamiliarTag : FamiliarTag
{
	// TODO: Innate feats and specific familiars
	// See Kholo Ancestry for a specific familiar
	/*public class FamiliarFeat : Feat
	{
		public int InnateFeatsCount { get; }
	
		public FamiliarFeat(string familiarKind, int innateFeatsCount) 
			: base(ModManager.RegisterFeatName($"Familiar{familiarKind}", familiarKind), $"{familiarKind} Familiar.", 
				$"You gain a {familiarKind} familiar.", [ModData.Traits.TFamiliar], null)
		{
			InnateFeatsCount = innateFeatsCount;
		}
	}*/

	/// <summary>
	/// If this familiar should gain any specific changes when spawned, such as replacing its creature type trait due to a familiar ability, then this action applies those modifications after the familiar has been fully created and given all of its QEffects, but before being spawned.
	/// </summary>
	public Action<Creature> OnFamiliarSpawn { set; get; } = familiar => { };
	
	/// <summary>
	/// If set, then the familiar's special bonuses which rely on your spellcasting ability will use the ability you set. If unset, then it will use your best spellcasting ability from all your spellcasting sources.
	/// </summary>
	public Ability? SpellcastingAbility { set; get; }

	public DeployableFamiliarTag WithOnFamiliarSpawn(Action<Creature> onSpawn)
	{
		OnFamiliarSpawn += onSpawn;
		return this;
	}

	public void Spawn(Creature master)
    {
	    Creature familiar = CreateCreature(master);
	    familiar.InitiativeControlledBy = master;
	    familiar.LongTermEffects = new LongTermEffects();
	    familiar.LongTermEffects.BeginningOfCombat(familiar);
	    familiar.LongTermEffects.Effects.Clear();
	    OnFamiliarSpawn.Invoke(familiar);
	    master.Battle.SpawnCreature(familiar, master.OwningFaction, master.Occupies);
    }
    
    private Creature CreateCreature(Creature master)
	{
		int level = master.Level;
		int specialBonus = Math.Max(
			3,
			SpellcastingAbility is null
				? master.Spellcasting?.Sources
					  .Max(src => src.SpellcastingAbilityModifier) 
				  ?? 0
				: master.Abilities.Get((Ability)SpellcastingAbility));
		Creature familiar = new Creature(
				IllustrationOrDefault,
				FamiliarName ?? $"{master.Name}'s Familiar",
				[Trait.Animal, Trait.Minion, Trait.Small, Trait.NoPhysicalUnarmedAttack],
				level, level + specialBonus, 5,
				new Defenses(
					master.Defenses.GetBaseValue(Defense.AC), 
					master.Defenses.GetBaseValue(Defense.Fortitude), 
					master.Defenses.GetBaseValue(Defense.Reflex), 
					master.Defenses.GetBaseValue(Defense.Will)),
				level * 5,
				// RAW familiars don't have their own attribute modifiers, but not assigning them would probably cause a bunch of errors
				new Abilities(
					master.Abilities.Strength,
					master.Abilities.Dexterity,
					master.Abilities.Constitution,
					master.Abilities.Intelligence,
					master.Abilities.Wisdom,
					master.Abilities.Charisma),
				new Skills(
					level + specialBonus,
					level,
					level,
					level,
					level,
					level,
					level,
					level,
					level,
					level,
					level,
					level,
					level,
					level + specialBonus,
					level,
					level))
			.WithCharacteristics(false, true)
			.WithEntersInitiativeOrder(false)
			.AddQEffect(MakeFamiliar(master));

		// Add familiar abilities to the familiar.
		// TODO: This will result in some amount of wonky behavior until each individual familiar ability is gone through.
		// BUG: Crashes. Because some feats assume a sheet, causing errors.
		/*if (master.PersistentCharacterSheet is {} sheet)
			foreach (Feat ft in sheet.Calculated.AllFeats
				         .Where(ft => ft.HasTrait(Trait.CombatFamiliarAbility)))
				familiar.WithFeat(ft.FeatName);*/

		return familiar;
	}

    /// <summary>
    /// Designates a creature as a familiar and adds familiar mechanics specific to QEffects (so not other features like the Minion trait).
    /// </summary>
    /// <param name="master"></param>
    /// <returns></returns>
	private QEffect MakeFamiliar(Creature master)
	{
		/*
		 * Mechanics not included:
		 * - Unable to benefit from item bonuses to its modifiers
		 * - The ability to gain reactions from specific abilities (base game limitation)
		 * - Low-light vision (no base game mechanic)
		 */
		
		return new QEffect(
			"Familiar",
			$$"""You can't act except when Commanded by {{master.Illustration.IllustrationAsIconString}} {Blue}{{master}}{/Blue} to take 2 actions, and you can't take reactions. Due to your size, checks aren't needed to move through your space or others' spaces. You still qualify for flanking.""")
		{
			Id = ModData.QEffectIds.FamiliarCreature, // Indicates a familiar creature.
			Source = master,
			Tag = this,
			StateCheck = qfThis =>
			{
				#region Tiny-like mechanics

				// Doesn't impede movement
				// Adding the ID, but not the Trait, has better behavior.
				qfThis.Owner.AddQEffect(new QEffect()
				{
					Id = QEffectId.Incorporeal,
					ExpiresAt = ExpirationCondition.Ephemeral
				});
				
				// Make look tinier
				// Require size check to avoid overriding size increases.
				if (qfThis.Owner.Space.SizeCategory < 1)
				{
					float sizeMultiplier = 0.7f;
					if (qfThis.Owner.HasEffect(QEffectId.Unconscious))
						sizeMultiplier -= 0.2f;
					else if (qfThis.Owner.HasEffect(QEffectId.Prone))
						sizeMultiplier -= 0.1f;
					qfThis.Owner.AnimationData.ChangeSizeTo(sizeMultiplier);
				}

				#endregion
				
				// Can't take reactions
				// Probably redundant due to base game implementations.
				qfThis.Owner.AddQEffect(new QEffect()
				{
					Id = QEffectId.CannotTakeReactions,
					ExpiresAt = ExpirationCondition.Ephemeral
				});
				
				// Can't hold items
				if (!qfThis.Owner.HasEffect(ModData.QEffectIds.FamiliarCanManipulate))
				{
					// ".ToList()" avoids modified collection errors.
					foreach (var item in qfThis.Owner.HeldItems.ToList())
					{
						qfThis.Owner.DropItem(item);
						qfThis.Owner.Overhead(
							"*can't hold items*",
							Color.White,
							qfThis.Owner + " can't hold items and dropped {b}" + item.Name + "{/b}");
					}
				}

				if (qfThis.Owner.HasEffect(QEffectId.Dying)
				    || !qfThis.Owner.Battle.InitiativeOrder.Contains(qfThis.Owner))
					return;
				
				Creature owner = qfThis.Owner;
				int index = (owner.Battle.InitiativeOrder.IndexOf(owner) + 1) % owner.Battle.InitiativeOrder.Count;
				
				Creature creature = owner.Battle.InitiativeOrder[index];
				owner.Actions.HasDelayedYieldingTo = creature;
				if (owner.Battle.CreatureControllingInitiative == owner)
					owner.Battle.CreatureControllingInitiative = creature;
				owner.Battle.InitiativeOrder.Remove(qfThis.Owner);
			},
			PreventTakingAction = action =>
			{
				// No attacks, except Escape or Force Open
				if (action.HasTrait(Trait.Attack)
				    && (action.ActionId is not ActionId.Escape || !action.Name.ToLower().Contains("force open")))
					return "Familiars can't attack except to Escape or Force Open";
				
				// Can't use manipulates or items
				if (action.HasTrait(Trait.Manipulate)
				    && !action.Owner.HasEffect(ModData.QEffectIds.FamiliarCanManipulate))
					return "Familiars can't take manipulate actions";
				
				if (action.Item is not null)
					return "Familiars can't use items";
				
				// Can't command other creatures
				if (action.ActionId is ActionId.CommandAnimalCompanion
					    or ActionId.CommandElemental
				    || action.ActionId == ModData.ActionIds.CommandFamiliar)
					return "Minions can't control other creatures";
				
				return null;
			},
			WhenMonsterDies = _ =>
			{
				master.AddQEffect(new QEffect("Dead Familiar",
					"Your familiar has died. It will reappear upon your next long rest.")
				{
					Id = ModData.QEffectIds.YourFamiliarIsDead
				});
				master.LongTermEffects ??= new LongTermEffects();
				if (master.LongTermEffects.Effects.FirstOrDefault(lt => lt.Id == ModData.LongTermEffects.LDeadFamiliar?.Id) == null)
					master.LongTermEffects.Add(ModData.LongTermEffects.LDeadFamiliar!);
			}
		};
	}

	#region Static Methods

	public static DeployableFamiliarTag? FindTag(Creature master)
	{
		return master.PersistentCharacterSheet is not null
			? FindTag(master.PersistentCharacterSheet.Calculated)
			: null;
	}

	public static DeployableFamiliarTag? FindTag(CalculatedCharacterSheetValues values)
	{
		return values.Tags.GetValueOrDefault(Familiars.FAMILIAR_KEY) as DeployableFamiliarTag;
	}

	public static bool IsFamiliarDead(Creature master) =>
		master.HasEffect(ModData.QEffectIds.YourFamiliarIsDead);

	public static Creature? FindMaster(Creature familiar) =>
		familiar.FindQEffect(ModData.QEffectIds.FamiliarCreature)?.Source;

	public static Creature? FindFamiliar(Creature master) =>
		master.Battle.AllCreatures.FirstOrDefault(familiar =>
			familiar.QEffects.Any(qf =>
				qf.Id == ModData.QEffectIds.FamiliarCreature
				&& qf.Source == master
				&& qf.Tag == FindTag(master)));

	#endregion
}