using System;
using System.Collections.Generic;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Witch;

public abstract class BasicLesson : Feat
{
	public static Trait TBasicLesson = ModManager.RegisterTrait("Basic Lesson");
	
	protected BasicLesson(FeatName featName, string? flavorText, string rulesText, List<Feat>? subfeats) 
		: base(featName, flavorText, rulesText, [TBasicLesson], subfeats)
	{
	}
}

public class NightsTerror : BasicLesson
{
	private static FeatName _featName = ModManager.RegisterFeatName("NightsTerror", "Lesson of Night's Terror");
	private static string _flavorText = "";
	private static string _rulesText = "";
	private static List<Feat> _subFeats = new();

	private NightsTerror() 
		: base(_featName, _flavorText, _rulesText, _subFeats)
	{
	}

	public static Feat Create()
	{
		return new NightsTerror()
			.WithOnSheet(sheet =>
			{
				var hexCantrip = AllSpells.CreateModernSpell(WitchSpells.ShroudOfNight, null, sheet.MaximumSpellLevel,
					false, new SpellInformation
					{
						ClassOfOrigin = WitchLoader.TWitch
					});
				
				var focusSpells = sheet.FocusSpells.GetOrCreate(WitchLoader.TWitch, () => new FocusSpells(Ability.Intelligence));
				focusSpells.Spells.Add(hexCantrip);
			});
	}
}