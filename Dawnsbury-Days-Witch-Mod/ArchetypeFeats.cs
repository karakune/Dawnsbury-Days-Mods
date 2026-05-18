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
	    List<Feat> patrons = (AllFeats.GetFeatByFeatName(WitchModData.FeatNames.Witch).Subfeats ?? new List<Feat>()).OfType<WitchPatronFeat>().Select(patron =>
	    {
		    return new Feat(MulticlassArchetypeFeats.SubclassToArchetype(patron), patron.FlavorText, 
				    $"Your spellcasting tradition is {patron.Tradition} and you become trained in {patron.Skill}.", 
				    new List<Trait>(), null)
			    .WithIllustration(patron.Illustration)
			    .WithOnSheet(sheet =>
			    {
				    sheet.TrainInThisOrSubstitute(patron.Skill);
				    
				    // We don't use MulticlassArchetypeFeats.SetupPreparedSpellcasting because we only get one cantrip slot
				    sheet.SpellTraditionsKnown.Add(patron.Tradition);
				    sheet.SetProficiency(Trait.Spell, Proficiency.Trained);
				    var preparedSpellSlots = new PreparedSpellSlots(Ability.Intelligence, patron.Tradition, WitchModData.Traits.Witch);
				    if (!sheet.PreparedSpells.TryAdd(WitchModData.Traits.Witch, preparedSpellSlots))
					    return;
				    
				    preparedSpellSlots.Slots.Add(new FreePreparedSpellSlot(0, WitchModData.Traits.Witch.ToStringOrTechnical() + "ArchetypeCantrip1"));
			    });
	    }).ToList();
	    
	    var dedicationFeat = Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes.ArchetypeFeats.CreateMulticlassDedication(WitchModData.Traits.Witch, "You have heard the whispers of a distant patron, who sent an emissary to teach you powerful magic.", 
			    "You cast spells like a witch.\nChoose a patron; you gain a familiar, but aside from from your chosen patron's tradition, you don't gain any other effects the patron would usually grant. Your familiar gains the normal number of abilities for a familiar instead of those a witch normally gets.\n"
			    + "\nYou gain the Cast a Spell activity.\nYou can prepare one cantrip each day from your familiar.\nYou're trained in the spell attack modifier and spell DC statistics.\nYour key spellcasting attribute for witch archetype spells is Intelligence, and they are witch spells of your patron's tradition.\nYou become trained in the skill associated with the patron's tradition; if you were already trained in it, you instead become trained in a skill of your choice.",
			    patrons)
			.WithDemandsAbility14(Ability.Intelligence)
			.WithOnSheet(sheet =>
		    {
			    sheet.GrantFeat(FeatName.ClassFamiliar);
		    });

	    yield return dedicationFeat;
	    
	    foreach (Feat spellcastingFeat in MulticlassArchetypeFeats.CreateSpellcastingFeats(WitchModData.Traits.Witch, Trait.Prepared, "Patron's", dedicationFeat.FeatName))
	      yield return spellcastingFeat;
	    foreach (Feat grantingArchetypeFeat in Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes.ArchetypeFeats.CreateBasicAndAdvancedMulticlassFeatGrantingArchetypeFeats(WitchModData.Traits.Witch, "Witchcraft"))
		    yield return grantingArchetypeFeat;
	}
}