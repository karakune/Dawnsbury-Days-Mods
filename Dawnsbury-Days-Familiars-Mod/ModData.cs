using Dawnsbury.Campaign.LongTerm;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.DeployableFamiliars;

public static class ModData
{
    public const string IdPrepend = "Familiars.";

    public static Trait ModTrait;

    /// <summary>
    /// Loads all mod data. This should typically be called by a mod before anything else.
    /// </summary>
    /// <para>
    /// When registering mod data, certain data must be called through the execution of lines of code, rather than assigned in their initialization. The Initializer skips these data until they're first called, which can result in errors due to out of order registration calls (especially when another mod isn't using <see cref="ModManager.TryParse"/>).
    /// </para>
    /// <para>The following data forms are typically safe due to the way Dawnsbury Days loads mods (or because their initialization nearly always gets called before errors could arise): <see cref="FeatName"/>, <see cref="Illustration"/>, <see cref="Trait"/>, <see cref="SfxNames"/>, <see cref="SpellId"/>. Tooltips from <see cref="ModManager.RegisterInlineTooltip(string, string)"/> likely aren't safe to assign as part of the initializer, but they typically shouldn't be shared between mods either.
    /// </para>
    /// <para>
    /// In general, trigger the initializer by separating declaration and assignment for the following data forms:
    /// <list type="bullet">
    /// <item>All other enums (e.g. <see cref="ActionId"/>, <see cref="QEffectId"/>)</item>
    /// <item>Mod settings registered with <see cref="ModManager.RegisterBooleanSettingsOption"/></item>
    /// </list>
    /// </para>
    public static void LoadData()
    {
        ModTrait = ModManager.ModBeingLoadedTrait!.Value; // Known not null at this stage
        ActionIds.Initialize();
        LongTermEffects.Initialize();
        QEffectIds.Initialize();
    }

    public static class ActionIds
    {
        public static ActionId CommandFamiliar;
        public static ActionId DeployFamiliar;
        public static ActionId RetrieveFamiliar;
        
        public static void Initialize()
        {
            CommandFamiliar = ModManager.SafelyRegisterEnumMember<ActionId>("CommandFamiliar");
            DeployFamiliar = ModManager.SafelyRegisterEnumMember<ActionId>("DeployFamiliar");
            RetrieveFamiliar = ModManager.SafelyRegisterEnumMember<ActionId>("RetrieveFamiliar");
        }
    }

    public static class CommonRequirements
    {
        public static string? WhyCannotCommand(Creature self, bool isDirectCommand = false, bool mustBeAdjacentWhenDeployed = false)
        {
            if (DeployableFamiliarTag.FindTag(self) is not { } fTag)
                return "You don't have a familiar.";
            if (DeployableFamiliarTag.IsFamiliarDead(self))
                return "Your familiar is dead.";
            Creature? familiar = DeployableFamiliarTag.FindFamiliar(self);
            if (familiar is null)
            {
                if (isDirectCommand)
                    return "You must deploy your familiar to use this action.";
            }
            else
            {
                if (mustBeAdjacentWhenDeployed && familiar.DistanceTo(self) > 1)
                    return "Your familiar must be adjacent or not deployed";
                if (familiar.HasEffect(QEffectId.Paralyzed))
                    return "Your familiar is paralyzed.";
                if (familiar.HasEffect(QEffectId.Dying) || familiar.HasEffect(QEffectId.Unconscious))
                    return "Your familiar is unconscious.";
            }
            if (DeployableFamiliarTag.HasCommandedThisTurn(self))
                return "You already commanded your familiar this turn.";
            return null;
        }
    }

    public static class FeatGroups
    {
        public static readonly FeatGroup SpecificFamiliars = new FeatGroup("Specific Familiars", 0);
        public static readonly FeatGroup FamiliarAbilities = new FeatGroup("Familiar Abilities", 1);
        public static readonly FeatGroup MasterAbilities = new FeatGroup("Master Abilities", 2);
    }

    public static class FeatNames
    {
        /// <summary>
        /// Wizard Arcane Thesis class feature, Improved Familiar
        /// </summary>
        public static readonly FeatName ArcaneThesisImprovedFamiliar = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ArcaneThesisImprovedFamiliar",
            ["Improved Familiar Attunement"]);

        public static readonly FeatName AutoDeployNo = ModManager.SafelyRegisterEnumMember<FeatName>(
            IdPrepend + "FamiliarAutoDeployNo",
            ["No"]);
        public static readonly FeatName AutoDeployYes = ModManager.SafelyRegisterEnumMember<FeatName>(
            IdPrepend + "FamiliarAutoDeployYes",
            ["Yes"]);

        #region Familiar Abilities

        public static readonly FeatName Amphibious = ModManager.SafelyRegisterEnumMember<FeatName>(
            IdPrepend + "FamiliarAbilityAmphibious",
            ["Amphibious"]);
        public static readonly FeatName Construct = ModManager.SafelyRegisterEnumMember<FeatName>(
            IdPrepend + "FamiliarAbilityConstruct",
            ["Construct"]);
        public static readonly FeatName Dragon = ModManager.SafelyRegisterEnumMember<FeatName>(
            IdPrepend + "FamiliarAbilityDragon",
            ["Dragon"]);
        public static readonly FeatName Echolocation = ModManager.SafelyRegisterEnumMember<FeatName>(
            IdPrepend + "FamiliarAbilityEcholocation",
            ["Echolocation"]);
        public static readonly FeatName FastMovement = ModManager.SafelyRegisterEnumMember<FeatName>(
            IdPrepend + "FamiliarAbilityFastMovement",
            ["Fast Movement"]);
        public static readonly FeatName Flier = ModManager.SafelyRegisterEnumMember<FeatName>(
            IdPrepend + "FamiliarAbilityFlier",
            ["Flier"]);
        public static readonly FeatName Independent = ModManager.SafelyRegisterEnumMember<FeatName>(
            IdPrepend + "FamiliarAbilityIndependent",
            ["Independent"]);
        public static readonly FeatName ManualDexterity = ModManager.SafelyRegisterEnumMember<FeatName>(
            IdPrepend + "FamiliarAbilityManualDexterity",
            ["Manual Dexterity"]);
        public static readonly FeatName Plant = ModManager.SafelyRegisterEnumMember<FeatName>(
            IdPrepend + "FamiliarAbilityPlant",
            ["Plant"]);
        public static readonly FeatName Tough = ModManager.SafelyRegisterEnumMember<FeatName>(
            IdPrepend + "FamiliarAbilityTough",
            ["Tough"]);

        #endregion
    }
    
    // TODO: token illustrations
    public static class Illustrations
    {
        public const string ModFolder = "FamiliarsAssets/";
        
        public static readonly ModdedIllustration FamiliarCauldron = new(ModFolder+"Cauldron.png");
        public static readonly ModdedIllustration FamiliarCrow = new(ModFolder+"Crow.png");
        public static readonly ModdedIllustration FamiliarFrog = new(ModFolder+"Frog.png");
    }

    public static class LongTermEffects
    {
        public static class WellKnownIDs
        {
            public const string DeadFamiliar = "DeadFamiliar";
        }

        public static void Initialize()
        {
            Dawnsbury.Campaign.LongTerm.LongTermEffects.EasyRegister(
                WellKnownIDs.DeadFamiliar,
                LongTermEffectDuration.UntilLongRest,
                YourFamiliarIsDead);
        }

        public static QEffect YourFamiliarIsDead()
        {
            return new QEffect(
                "Dead Familiar",
                "Your familiar has died. It will reappear upon your next long rest.")
            {
                Id = ModData.QEffectIds.YourFamiliarIsDead,
                LongTermEffectDuration = LongTermEffectDuration.UntilLongRest,
                StartOfCombat = async qfThis =>
                    qfThis.Owner.Overhead(
                        "no familiar",
                        Color.Green,
                        qfThis.Owner + "'s familiar is dead. It will reappear upon your next long rest."),
                EndOfCombat = async (qfThis, b) =>
                    qfThis.Owner.LongTermEffects?.Add(WellKnownLongTermEffects.CreateLongTermEffect(WellKnownIDs.DeadFamiliar)!)
            };
        }
    }

    public static class QEffectIds
    {
        /// <summary>
        /// A creature with this QEffectId is a creature who is a familiar.
        /// </summary>
        public static QEffectId FamiliarCreature;
        public static QEffectId FamiliarDeployed;
        public static QEffectId YourFamiliarIsDead;
        public static QEffectId FamiliarCanManipulate;
        public static QEffectId FamiliarEcholocation;
        /// <summary>
        /// A familiar with this QEffectId can cast scrolls with any of the traits in <see cref="QEffect.Traits"/>. The traditions of spells it can cast should be put into this list.
        /// </summary>
        public static QEffectId FamiliarCanUseScrolls;

        public static void Initialize()
        {
            // TODO: Document each of these IDs.
            
            FamiliarCreature = ModManager.SafelyRegisterEnumMember<QEffectId>("FamiliarCreature");
            FamiliarDeployed = ModManager.SafelyRegisterEnumMember<QEffectId>("FamiliarDeployed");
            YourFamiliarIsDead = ModManager.SafelyRegisterEnumMember<QEffectId>("YourFamiliarIsDead");
            FamiliarCanManipulate =  ModManager.SafelyRegisterEnumMember<QEffectId>("FamiliarCanManipulate");
            FamiliarEcholocation =  ModManager.SafelyRegisterEnumMember<QEffectId>("FamiliarEcholocation");
        }
    }

    public static class Traits
    {
        /// <summary>
        /// If a modded feat grants a combat familiar, adding this trait will automatically convert that feat to grant a deployable familiar. Do not add this trait if the feat grants a familiar indirectly by granting the <see cref="FeatName.ClassFamiliar"/> or <see cref="FeatName.AnimalAccomplice"/> feats.
        /// </summary>
        public static readonly Trait DeployableFamiliarFeat = ModManager.RegisterTrait("DeployableFamiliarFeat", new TraitProperties("Deployable Familiar Feat", false));
        
        public static readonly Trait FamiliarDeploy = ModManager.RegisterTrait("FamiliarDeploy", new TraitProperties("", relevant: false));
    }

    extension(ModManager)
    {
        /// <summary>
        /// Registers the source enum to the game, or returns the original if it's already registered.
        /// </summary>
        /// <param name="technicalName">The technicalName string of the enum being registered. If registering a trait, this is the displayName, according to the parameter specifications of <see cref="ModManager.RegisterTrait"/>.</param>
        /// <param name="extraParams">An array of optional parameters. For a <see cref="FeatName"/>, the first parameter is a human-readable display name. For a <see cref="Trait"/>, the first parameter is a <see cref="TraitProperties"/>.</param>
        /// <typeparam name="T">The enum type such as <see cref="SpellId"/>, <see cref="FeatName"/>, or <see cref="QEffectId"/>.</typeparam>
        /// <returns>The newly registered enum.</returns>
        public static T SafelyRegisterEnumMember<T>(string technicalName, object[]? extraParams = null) where T : struct, Enum
        {
            bool alreadyRegistered = ModManager.TryParse(technicalName, out T oldRegistration);
            
            if (alreadyRegistered)
                return oldRegistration;
            
            Type type = typeof(T);
            if (type == typeof(FeatName))
                return (T)(Enum)ModManager.RegisterFeatName(technicalName, (string?)extraParams?[0]);
            if (type == typeof(Trait))
                return (T)(Enum)ModManager.RegisterTrait(technicalName, (TraitProperties?)extraParams?[0]);
            else
                return ModManager.RegisterEnumMember<T>(technicalName);
        }
    }
}