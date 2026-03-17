using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes.Multiclass;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Witch;

public static class ArchetypeFeats
{
	public static IEnumerable<Feat> CreateFeats()
	{
	    List<Feat> patrons = (AllFeats.GetFeatByFeatName(WitchLoader.FNWitch).Subfeats ?? new List<Feat>()).OfType<WitchPatronFeat>().Select(patron =>
	    {
		    return new Feat(MulticlassArchetypeFeats.SubclassToArchetype(patron), patron.FlavorText, 
				    "You gain the Cast a Spell activity. You can prepare one cantrip each day from your familiar. You're trained in the spell attack modifier and spell DC statistics. Your key spellcasting attribute for witch archetype spells is Intelligence, and they are witch spells of your patron's tradition. You become trained in the skill associated with the patron's tradition; if you were already trained in it, you instead become trained in a skill of your choice.", 
				    new List<Trait>(), null)
			    .WithIllustration(patron.Illustration)
			    .WithOnSheet(sheet =>
			    {
				    sheet.TrainInThisOrSubstitute(patron.Skill);
				    
				    // We don't use MulticlassArchetypeFeats.SetupPreparedSpellcasting because we only get one cantrip slot
				    sheet.SpellTraditionsKnown.Add(patron.Tradition);
				    sheet.SetProficiency(Trait.Spell, Proficiency.Trained);
				    var preparedSpellSlots = new PreparedSpellSlots(Ability.Intelligence, patron.Tradition);
				    if (!sheet.PreparedSpells.TryAdd(WitchLoader.TWitch, preparedSpellSlots))
					    return;
				    
				    preparedSpellSlots.Slots.Add(new FreePreparedSpellSlot(0, WitchLoader.TWitch.ToStringOrTechnical() + "ArchetypeCantrip1"));
			    });
	    }).ToList();
	    yield return Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes.ArchetypeFeats.CreateMulticlassDedication(WitchLoader.TWitch, "You have heard the whispers of a distant patron, who sent an emissary to teach you powerful magic.", 
			    "You cast spells like a witch. Choose a patron; you gain a familiar, but aside from from your chosen patron's tradition, you don't gain any other effects the patron would usually grant. Your familiar gains the normal number of abilities for a familiar instead of those a witch normally gets.",
			    patrons)
			.WithDemandsAbility14(Ability.Intelligence)
			.WithOnSheet(sheet =>
		    {
			    sheet.GrantFeat(FeatName.ClassFamiliar);
		    });
	    
	    foreach (Feat spellcastingFeat in MulticlassArchetypeFeats.CreateSpellcastingFeats(WitchLoader.TWitch, Trait.Prepared, "Patron's"))
	      yield return spellcastingFeat;
	    foreach (Feat grantingArchetypeFeat in Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes.ArchetypeFeats.CreateBasicAndAdvancedMulticlassFeatGrantingArchetypeFeats(WitchLoader.TWitch, "Witchcraft"))
		    yield return grantingArchetypeFeat;
	}
}