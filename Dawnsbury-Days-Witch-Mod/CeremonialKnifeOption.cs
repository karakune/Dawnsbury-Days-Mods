using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Selections.Selected;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Phases.Menus.CharacterBuilderPages;

namespace Dawnsbury.Mods.Classes.Witch;


public class CeremonialKnifeOption : AbstractSpellSelectionOption
{
	public const string CEREMONIAL_KNIFE_TAG_SPELL = "CeremonialKnifeSpell";

	public string Tag { get; }

	public CeremonialKnifeOption(string key, string name, int level, int maximumSpellLevel)
		: base(key, name, level, maximumSpellLevel)
	{
		Tag = CEREMONIAL_KNIFE_TAG_SPELL;
		OptionLevel = MORNING_PREPARATIONS_LEVEL;
	}

	public override int MaximumNumberOfSpells => 1;

	public override SpellSelectionPageKind SpellSelectionPageKind => SpellSelectionPageKind.Standard;

	public override bool Eligible(CalculatedCharacterSheetValues values, Spell spell)
	{
		if (spell.HasTrait(Trait.Cantrip) || spell.HasTrait(Trait.Focus) || spell.HasTrait(Trait.SpellCannotBeChosenInCharacterBuilder))
			return false;

		if (values.AllFeats.FirstOrDefault(f => f is WitchPatronFeat) is not WitchPatronFeat witchPatronFeat)
			return false;

		if (!spell.HasTrait(witchPatronFeat.Tradition) && !values.PreparedSpells[WitchModData.Traits.Witch].AdditionalPreparableSpells.Contains(spell.SpellId))
			return false;
		
		return MaximumSpellLevel >= 1 && spell.MinimumSpellLevel <= MaximumSpellLevel;
	}

	public override bool IsTaken(CalculatedCharacterSheetValues values, Spell spell)
	{
		return values.Tags.TryGetValue(Tag, out var obj) && obj is SpellId spellId && spellId == spell.SpellId;
	}

	public override bool IsComplete(SpellSelectedChoice spellSelectedChoice)
	{
		return spellSelectedChoice.Choices.Count == 1;
	}

	public override void Apply(
		SpellSelectedChoice spellSelectedChoice,
		CalculatedCharacterSheetValues sheet)
	{
		if (!spellSelectedChoice.Choices.Any())
			return;
		sheet.Tags[Tag] = spellSelectedChoice.Choices[0].SpellId;
	}
}