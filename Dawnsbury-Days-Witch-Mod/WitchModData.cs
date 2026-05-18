using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Witch;

public static class WitchModData
{
	public static class FeatNames
	{
		public static FeatName Witch = ModManager.RegisterFeatName("FeatWitch", "Witch");
		
		public static FeatName StarlessShadow = ModManager.RegisterFeatName("StarlessShadow", "Starless Shadow");
		public static FeatName FaithsFlamekeeper = ModManager.RegisterFeatName("FaithsFlamekeeper", "Faith's Flamekeeper");
		public static FeatName SpinnerOfThreads = ModManager.RegisterFeatName("SpinnerOfThreads", "Spinner of Threads");
		public static FeatName SilenceInSnow = ModManager.RegisterFeatName("SilenceInSnow", "Silence in Snow");
		public static FeatName InscribedOne = ModManager.RegisterFeatName("InscribedOne", "The Inscribed One");
		
		public static FeatName StalkingNight = ModManager.RegisterFeatName("WiFamStalkingNight", "Familiar of Stalking Night");
		public static FeatName RestoredSpirit = ModManager.RegisterFeatName("WiFamRestoredSpirit", "Familiar of Restored Spirit");
		public static FeatName FreezingRime = ModManager.RegisterFeatName("WiFamFreezingRime", "Familiar of Freezing Rime");
		public static FeatName BalancedLuck = ModManager.RegisterFeatName("WiFamBalancedLuck", "Familiar of Balanced Luck");
		public static FeatName FlowingScript = ModManager.RegisterFeatName("WiFamFlowingScript", "Familiar of Flowing Script");
		public static FeatName OngoingMisery = ModManager.RegisterFeatName("WiFamOngoingMisery", "Familiar of Ongoing Misery");
		public static FeatName KeenSenses = ModManager.RegisterFeatName("WiFamKeenSenses", "Familiar of Keen Senses");
	}

	public static class QEffectIds
	{
		public static QEffectId NudgeFateId = ModManager.RegisterEnumMember<QEffectId>("NudgeFate");
		public static QEffectId DiscernSecretsId = ModManager.RegisterEnumMember<QEffectId>("DiscernSecrets");
		public static QEffectId DiscernSecretsUsedThisTurnId = ModManager.RegisterEnumMember<QEffectId>("DiscernSecretsUsedThisTurn");
		public static QEffectId DiscernSecretsImmunityId = ModManager.RegisterEnumMember<QEffectId>("DiscernSecretsImmunity");
		public static QEffectId PatronsPuppetId = ModManager.RegisterEnumMember<QEffectId>("PatronsPuppet");
		public static QEffectId PatronsPuppetUsedThisTurnId = ModManager.RegisterEnumMember<QEffectId>("PatronsPuppetUsedThisTurn");
	}
	
	public static class Traits
	{
		public static Trait ModName = ModManager.RegisterModNameTrait("RemasteredWitch", "Remastered Witch");
		public static Trait Hex = ModManager.RegisterTrait("Hex");
		public static Trait FirstHex = ModManager.RegisterTrait("First Hex", new TraitProperties("", relevant: false));
		public static Trait Witch = ModManager.RegisterTrait("Witch", new TraitProperties("Witch", true)
		{
			IsClassTrait = true
		});
	}
}