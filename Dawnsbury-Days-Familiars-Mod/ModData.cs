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
        ActionIds.Initialize();
        BooleanOptions.Initialize();
        LongTermEffects.Initialize();
        QEffectIds.Initialize();
    }

    /// <summary>
    /// Registers the source enum to the game, or returns the original if it's already registered.
    /// </summary>
    /// <param name="technicalName">The technicalName string of the enum being registered.</param>
    /// <typeparam name="T">The enum being registered to.</typeparam>
    /// <returns>The newly registered enum.</returns>
    public static T SafelyRegister<T>(string technicalName) where T : struct, Enum
    {
        return ModManager.TryParse(technicalName, out T alreadyRegistered)
            ? alreadyRegistered
            : ModManager.RegisterEnumMember<T>(technicalName);
    }

    public static class ActionIds
    {
        public static ActionId CommandFamiliar;
        
        public static void Initialize()
        {
            CommandFamiliar = SafelyRegister<ActionId>("CommandFamiliar");
        }
    }

    // TODO: Use or remove BooleanOptions
    /// <summary>
    /// Keeps the options registered with <see cref="ModManager.RegisterBooleanSettingsOption"/>. To read the registered options, use <see cref="PlayerProfile.Instance.IsBooleanOptionEnabled(string)"/>.
    /// </summary>
    public static class BooleanOptions
    {
        //public static string UnrestrictedTrace = null!;
        
        public static void Initialize()
        {
            /*UnrestrictedTrace = RegisterBooleanOption(
                IdPrepend+"UnrestrictedTrace",
                "Runesmith: Less Restrictive Rune Tracing",
                "Enabling this option removes protections against \"bad decisions\" with tracing certain runes on certain targets.\n\nThe Runesmith is a class on the more advanced end of tactics and creativity. For example, you might want to trace Esvadir onto an enemy because you're about to invoke it onto a different, adjacent enemy. Or you might trace Atryl on yourself as a 3rd action so that you can move it with Transpose Etching (just 1 action) on your next turn, because you're a ranged build.\n\nThis option is for those players.",
                true);*/
        }
        
        /// <summary>
        /// Functions as <see cref="ModManager.RegisterBooleanSettingsOption"/>, but also returns the technicalName.
        /// </summary>
        /// <returns>(string) The technical name for the option.</returns>
        public static string RegisterBooleanOption(
            string technicalName,
            string caption,
            string longDescription,
            bool defaultValue)
        {
            ModManager.RegisterBooleanSettingsOption(technicalName, caption, longDescription, defaultValue);
            return technicalName;
        }
    }

    public static class CommonRequirements
    {
        public static string? WhyCannotCommand(Creature self, bool isDirectCommand = false)
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
                if (familiar.HasEffect(QEffectId.Paralyzed))
                    return "Your familiar is paralyzed.";
                if (familiar.HasEffect(QEffectId.Dying) || familiar.HasEffect(QEffectId.Unconscious))
                    return "Your familiar is unconscious.";
            }
            if (self.FindQEffect(QEffectId.FamiliarAbility) is { UsedThisTurn: true })
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
        /// Witch Familiar class feature
        /// </summary>
        public static readonly FeatName WitchFamiliarBoost = ModManager.RegisterFeatName("WitchFamiliarBoost");
        /// <summary>
        /// Wizard Arcane Thesis class feature, Improved Familiar
        /// </summary>
        public static readonly FeatName ArcaneThesisImprovedFamiliar = ModManager.RegisterFeatName("ArcaneThesisImprovedFamiliar", "Improved Familiar Attunement");

        public static readonly FeatName AutoDeployNo = ModManager.RegisterFeatName(IdPrepend + "FamiliarAutoDeployNo", "No");
        public static readonly FeatName AutoDeployYes = ModManager.RegisterFeatName(IdPrepend + "FamiliarAutoDeployYes", "Yes");

        #region Familiar Abilities

        public static readonly FeatName Amphibious = ModManager.RegisterFeatName(IdPrepend + "FamiliarAbilityAmphibious", "Amphibious");
        public static readonly FeatName Construct = ModManager.RegisterFeatName(IdPrepend + "FamiliarAbilityConstruct", "Construct");
        public static readonly FeatName Dragon = ModManager.RegisterFeatName(IdPrepend + "FamiliarAbilityDragon", "Dragon");
        public static readonly FeatName FastMovement = ModManager.RegisterFeatName(IdPrepend + "FamiliarAbilityFastMovement", "Fast Movement");
        public static readonly FeatName Flier = ModManager.RegisterFeatName(IdPrepend + "FamiliarAbilityFlier", "Flier");
        public static readonly FeatName ManualDexterity = ModManager.RegisterFeatName(IdPrepend + "FamiliarAbilityManualDexterity", "Manual Dexterity");
        public static readonly FeatName Plant = ModManager.RegisterFeatName(IdPrepend + "FamiliarAbilityPlant", "Plant");
        public static readonly FeatName Tough = ModManager.RegisterFeatName(IdPrepend + "FamiliarAbilityTough", "Tough");

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

        public static LongTermEffect? LDeadFamiliar;

        public static void Initialize()
        {
            LDeadFamiliar = WellKnownLongTermEffects.CreateLongTermEffect(WellKnownIDs.DeadFamiliar);
            
            Dawnsbury.Campaign.LongTerm.LongTermEffects.EasyRegister(
                WellKnownIDs.DeadFamiliar,
                LongTermEffectDuration.UntilLongRest,
                () => new QEffect(
                    "Dead Familiar",
                    "Your familiar has died. It will reappear upon your next long rest.")
                {
                    Id = ModData.QEffectIds.YourFamiliarIsDead,
                    StartOfCombat = async qfThis =>
                        qfThis.Owner.Overhead(
                            "no familiar",
                            Color.Green,
                            qfThis.Owner + "'s familiar is dead. It will reappear upon your next long rest.")
                });
        }
    }

    public static class QEffectIds
    {
        /// <summary>
        /// A creature with this QEffectId is a creature who is a familiar.
        /// </summary>
        public static QEffectId FamiliarCreature;
        public static QEffectId FamiliarDeployed;
        public static QEffectId HasFamiliar;
        public static QEffectId YourFamiliarIsDead;
        public static QEffectId FamiliarCanManipulate;
        /// <summary>
        /// A familiar with this QEffectId can cast scrolls with any of the traits in <see cref="QEffect.Traits"/>. The traditions of spells it can cast should be put into this list.
        /// </summary>
        public static QEffectId FamiliarCanUseScrolls;

        public static void Initialize()
        {
            // TODO: Document each of these IDs.
            
            FamiliarCreature = ModManager.RegisterEnumMember<QEffectId>("FamiliarCreature");
            FamiliarDeployed = ModManager.RegisterEnumMember<QEffectId>("FamiliarDeployed");
            HasFamiliar = ModManager.RegisterEnumMember<QEffectId>("HasFamiliar"); // TODO: Doesn't do anything. Is an ID on the two deployment option feats, and nothing else.
            YourFamiliarIsDead = ModManager.RegisterEnumMember<QEffectId>("YourFamiliarIsDead");
            FamiliarCanManipulate =  ModManager.RegisterEnumMember<QEffectId>("FamiliarCanManipulate");
        }
    }

    public static class Traits
    {
        /// <summary>
        /// The name of the mod, for the purposes of branding feats.
        /// </summary>
        public static readonly Trait ModName = ModManager.RegisterTrait("DeployableFamiliars", new TraitProperties("Deployable Familiars", true));
        
        /// <summary>
        /// If a modded feat grants a combat familiar, adding this trait will automatically convert that feat to grant a deployable familiar. Do not add this trait if the feat grants a familiar indirectly by granting the <see cref="FeatName.ClassFamiliar"/> or <see cref="FeatName.AnimalAccomplice"/> feats.
        /// </summary>
        public static readonly Trait DeployableFamiliarFeat = ModManager.RegisterTrait("DeployableFamiliarFeat", new TraitProperties("Deployable Familiar Feat", false));
        
        // TODO: This is the trait added to the deployment toggles. Consider removing or renaming.
        public static readonly Trait FamiliarDeploy = ModManager.RegisterTrait("FamiliarDeploy", new TraitProperties("", relevant: false));
    }
}